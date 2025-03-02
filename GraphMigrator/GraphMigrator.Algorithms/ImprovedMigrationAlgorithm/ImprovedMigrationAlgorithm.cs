using GraphMigrator.Algorithms.Neo4jDataLayer;
using GraphMigrator.Algorithms.RelationalSchemaExtractors;
using GraphMigrator.Domain.Configuration;
using GraphMigrator.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GraphMigrator.Algorithms.ImprovedMigrationAlgorithmN;

// TODO consider complex primary keys ??
// TODO consider user settings (user can make table with three foreign keys as entity table and vice versa)
// TODO create indices
public class ImprovedMigrationAlgorithm(
    IRelationalSchemaExtractor relationalSchemaExtractor,
    IOptions<SourceDataSourceConfiguration> configurationOptions,
    IServiceProvider serviceProvider) : IImprovedMigrationAlgorithm
{
    private readonly IRelationalSchemaExtractor _relationalSchemaExtractor = relationalSchemaExtractor;
    private readonly SourceDataSourceConfiguration configuration = configurationOptions.Value;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async Task MigrateToGraphDatabaseAsync()
    {
//#if !DEBUG
//        await MigrateToGraphDatabaseDebugVersion();
//#else
        var schema = await _relationalSchemaExtractor.GetSchema();

        var (entityTables, relationshipTables) = ClassifyTables(schema);

        var entityTasks = entityTables.Select(CreateNodesForTableAsync);
        await Task.WhenAll(entityTasks);

        var relationshipTasks = relationshipTables.Select(CreateRelationshipsForTableAsync);
        await Task.WhenAll(relationshipTasks);

        var foreignKeyTasks = entityTables.SelectMany(table =>
            table.ForeignKeys.Select(fk =>
                CreateRelationshipsForForeignKeyAsync(table, fk)));
        await Task.WhenAll(foreignKeyTasks);
//#endif
    }

    private async Task MigrateToGraphDatabaseDebugVersion()
    {
        var schema = await _relationalSchemaExtractor.GetSchema();

        var (entityTables, relationshipTables) = ClassifyTables(schema);

        foreach(var  entityTable in entityTables)
        {
            await CreateNodesForTableAsync(entityTable);
        }

        foreach (var table in relationshipTables)
        {
            await CreateRelationshipsForTableAsync(table);
        }

        var foreignKeyTasks = entityTables.SelectMany(table => table.ForeignKeys);
        foreach (var table in entityTables)
        {
            foreach (var foreignKey in table.ForeignKeys)
            {
                await CreateRelationshipsForForeignKeyAsync(table, foreignKey);
            }
        }
    }

    private async Task CreateNodesForTableAsync(TableSchema table)
    {
        var columnsToInclude = table.Columns
            .Where(c => !table.ForeignKeys
                              .Any(fk => fk.ColumnName == c.Name) || table.PrimaryKeys.Contains(c.Name))
            .Select(c => c.Name);

        var columnsStr = string.Join(", ", columnsToInclude);
        var query = $"SELECT {columnsStr} FROM [{table.Name}]";

        await using var sqlConnection = new SqlConnection(configuration.ConnectionString);
        await sqlConnection.OpenAsync();

        using var command = new SqlCommand(query, sqlConnection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var properties = new Dictionary<string, object>();
            foreach (var column in columnsToInclude)
            {
                var value = reader[column];
                if (value != DBNull.Value)
                    properties[column] = GetValue(value);
            }

            var cypher = $@"CREATE (n:{table.Name} $props) RETURN true";

            var neo4JDataAccess = _serviceProvider.GetRequiredService<INeo4jDataAccess>();

            await neo4JDataAccess.ExecuteWriteTransactionAsync<bool>(cypher, new
            {
                props = properties
            });
        }
    }

    private async Task CreateRelationshipsForTableAsync(TableSchema table)
    {
        var nonForeignKeyColumns = table.Columns
            .Where(c => !table.ForeignKeys.Any(fk => fk.ColumnName == c.Name))
            .Select(c => c.Name);

        var columnsStr = string.Join(", ", table.Columns.Select(c => c.Name));
        var query = $"SELECT {columnsStr} FROM [{table.Name}]";

        await using var sqlConnection = new SqlConnection(configuration.ConnectionString);
        await sqlConnection.OpenAsync();

        using var command = new SqlCommand(query, sqlConnection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var properties = new Dictionary<string, object>();
            foreach (var column in nonForeignKeyColumns)
            {
                var value = reader[column];
                if (value != DBNull.Value)
                    properties[column] = GetValue(value);
            }

            var fk1 = table.ForeignKeys[0];
            var fk2 = table.ForeignKeys[1];

            var cypher = $@"
                MATCH (source:{fk1.ReferencedTableName} {{{fk1.ReferencedColumnName}: $sourceId}})
                MATCH (target:{fk2.ReferencedTableName} {{{fk2.ReferencedColumnName}: $targetId}})
                CREATE (source)-[r:{table.Name} $props]->(target) RETURN true";

            var neo4JDataAccess = _serviceProvider.GetRequiredService<INeo4jDataAccess>();

            await neo4JDataAccess.ExecuteWriteTransactionAsync<bool>(cypher, new
            {
                sourceId = GetValue(reader[fk1.ColumnName]),
                targetId = GetValue(reader[fk2.ColumnName]),
                props = properties
            });
        }
    }

    private async Task CreateRelationshipsForForeignKeyAsync(TableSchema table, ForeignKeySchema fk)
    {
        var primaryIdColumnName = table.PrimaryKeys[0];
        var query = $"SELECT {primaryIdColumnName}, {fk.ColumnName} FROM [{table.Name}]";

        await using var sqlConnection = new SqlConnection(configuration.ConnectionString);
        await sqlConnection.OpenAsync();

        using var command = new SqlCommand(query, sqlConnection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var sourceId = GetValue(reader[primaryIdColumnName]);
            var targetId = GetValue(reader[fk.ColumnName]);

            var cypher = $@"
                MATCH (source:{table.Name} {{{primaryIdColumnName}: $sourceId}})
                MATCH (target:{fk.ReferencedTableName} {{{fk.ReferencedColumnName}: $targetId}})
                CREATE (target)-[r:HAS_{fk.ReferencedTableName}_{table.Name}]->(source) RETURN true";

            var neo4JDataAccess = _serviceProvider.GetRequiredService<INeo4jDataAccess>();

            await neo4JDataAccess.ExecuteWriteTransactionAsync<bool>(cypher, new
            {
                sourceId,
                targetId
            });
        }
    }

    private static (List<TableSchema> EntityTables, List<TableSchema> RelationshipTables) ClassifyTables(RelationalDatabaseSchema schema)
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

    private static bool IsRelationshipTable(TableSchema table)
    {
        if (table.ForeignKeys.Count != 2)
            return false;

        if (table.PrimaryKeys.Count == 1)
            return table.Columns.Count <= 4;

        if (table.PrimaryKeys.Count == 2)
            return table.Columns.Count <= 3;

        return false;
    }

    private static object GetValue(object value)
    {
        return value is Guid ? value.ToString() : value;
    }
}

