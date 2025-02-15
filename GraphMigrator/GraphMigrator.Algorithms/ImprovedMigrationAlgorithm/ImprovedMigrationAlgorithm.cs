using GraphMigrator.Algorithms.Neo4jDataLayer;
using GraphMigrator.Algorithms.RelationalSchemaExtractors;
using GraphMigrator.Domain.Configuration;
using GraphMigrator.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GraphMigrator.Algorithms.ImprovedMigrationAlgorithmN;

public class ImprovedMigrationAlgorithm(
    IRelationalSchemaExtractor relationalSchemaExtractor,
    INeo4jDataAccess neo4JDataAccess,
    IOptions<SourceDataSourceConfiguration> configurationOptions,
    IServiceProvider serviceProvider) : IImprovedMigrationAlgorithm
{
    private readonly IRelationalSchemaExtractor _relationalSchemaExtractor = relationalSchemaExtractor;
    private readonly INeo4jDataAccess _neo4JDataAccess = neo4JDataAccess;
    private readonly SourceDataSourceConfiguration configuration = configurationOptions.Value;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task MigrateToGraphDatabaseAsync()
    {
        var schema = await _relationalSchemaExtractor.GetSchema();

        // Крок 2: Класифікація таблиць
        var (entityTables, relationshipTables) = ClassifyTables(schema);

        // Крок 5: Паралельна обробка
        var entityTasks = entityTables.Select(CreateNodesForTableAsync);
        await Task.WhenAll(entityTasks);

        var relationshipTasks = relationshipTables.Select(table =>
            CreateRelationshipsForTableAsync(table));
        await Task.WhenAll(relationshipTasks);

        // Створення додаткових ребер на основі FK в таблицях сутностей
        var foreignKeyTasks = entityTables.SelectMany(table =>
            table.ForeignKeys.Select(fk =>
                CreateRelationshipsForForeignKeyAsync(table, fk)));
        await Task.WhenAll(foreignKeyTasks);
    }

    private (List<TableSchema> EntityTables, List<TableSchema> RelationshipTables) ClassifyTables(RelationalDatabaseSchema schema)
    {
        var entityTables = new List<TableSchema>();
        var relationshipTables = new List<TableSchema>();

        foreach (var table in schema.Tables)
        {
            if (IsRelationshipTable(table))
                relationshipTables.Add(table);
            else
                entityTables.Add(table);
        }

        return (entityTables, relationshipTables);
    }

    private bool IsRelationshipTable(TableSchema table)
    {
        // Правила класифікації таблиць зв'язування
        if (table.ForeignKeys.Count != 2)
            return false;

        // Випадок 1: Простий первинний ключ + два FK
        if (table.PrimaryKeys.Count == 1)
            return table.Columns.Count <= 4; // ID + 2 FK + максимум 1 додатковий атрибут

        // Випадок 2: Складений первинний ключ з двох FK
        if (table.PrimaryKeys.Count == 2)
            return table.Columns.Count <= 3; // 2 FK (вони ж PK) + максимум 1 додатковий атрибут

        return false;
    }

    private async Task CreateNodesForTableAsync(TableSchema table)
    {
        var columnsStr = string.Join(", ", table.Columns.Select(c => c.Name));
        var query = $"SELECT {columnsStr} FROM {table.Name}";

        await using var sqlConnection = new SqlConnection(configuration.ConnectionString);
        await sqlConnection.OpenAsync();

        using var command = new SqlCommand(query, sqlConnection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var properties = new Dictionary<string, object>();
            foreach (var column in table.Columns)
            {
                var value = reader[column.Name];
                if (value != DBNull.Value)
                    properties[column.Name] = value;
            }

            var cypher = $@"
                CREATE (n:{table.Name} $props)
                SET n.id = $id RETURN true";

            var neo4JDataAccess = _serviceProvider.GetRequiredService<INeo4jDataAccess>();

            await neo4JDataAccess.ExecuteWriteTransactionAsync<bool>(cypher, new
            {
                props = properties,
                id = reader[table.PrimaryKeys[0]].ToString()
            });
        }
    }

    private async Task CreateRelationshipsForTableAsync(TableSchema table)
    {
        var columnsStr = string.Join(", ", table.Columns.Select(c => c.Name));
        var query = $"SELECT {columnsStr} FROM {table.Name}";

        await using var sqlConnection = new SqlConnection(configuration.ConnectionString);
        await sqlConnection.OpenAsync();

        using var command = new SqlCommand(query, sqlConnection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var properties = new Dictionary<string, object>();
            foreach (var column in table.Columns.Where(c =>
                !table.ForeignKeys.Any(fk => fk.ColumnName == c.Name)))
            {
                var value = reader[column.Name];
                if (value != DBNull.Value)
                    properties[column.Name] = value;
            }

            var fk1 = table.ForeignKeys[0];
            var fk2 = table.ForeignKeys[1];

            var cypher = $@"
                MATCH (source:{fk1.ReferencedTableName} {{id: $sourceId}})
                MATCH (target:{fk2.ReferencedTableName} {{id: $targetId}})
                CREATE (source)-[r:{table.Name} $props]->(target) RETURN true";

            var neo4JDataAccess = _serviceProvider.GetRequiredService<INeo4jDataAccess>();

            await neo4JDataAccess.ExecuteWriteTransactionAsync<bool>(cypher, new
            {
                sourceId = reader[fk1.ColumnName].ToString(),
                targetId = reader[fk2.ColumnName].ToString(),
                props = properties
            });
        }
    }

    private async Task CreateRelationshipsForForeignKeyAsync(TableSchema table, ForeignKeySchema fk)
    {
        var cypher = $@"
            MATCH (source:{table.Name} {{id: $sourceId}})
            MATCH (target:{fk.ReferencedTableName} {{id: $targetId}})
            CREATE (source)-[r:HAS_{fk.Name}]->(target) RETURN true";

        var neo4JDataAccess = _serviceProvider.GetRequiredService<INeo4jDataAccess>();

        await neo4JDataAccess.ExecuteWriteTransactionAsync<bool>(cypher, new
        {
            sourceId = table.PrimaryKeys[0],
            targetId = fk.ReferencedColumnName
        });
    }
}

