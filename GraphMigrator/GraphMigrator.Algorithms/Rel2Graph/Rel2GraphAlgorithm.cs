using GraphMigrator.Algorithms.Neo4jDataLayer;
using GraphMigrator.Algorithms.RelationalSchemaExtractors;
using GraphMigrator.Domain.Configuration;
using GraphMigrator.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace GraphMigrator.Algorithms.Rel2Graph;

public class Rel2GraphAlgorithm(
    IRelationalSchemaExtractor relationalSchemaExtractor,
    IOptions<SourceDataSourceConfiguration> configurationOptions,
    INeo4jDataAccess neo4JDataAccess) : IRel2GraphAlgorithm
{
    private readonly IRelationalSchemaExtractor _relationalSchemaExtractor = relationalSchemaExtractor;
    private readonly SourceDataSourceConfiguration configuration = configurationOptions.Value;
    private readonly INeo4jDataAccess _neo4JDataAccess = neo4JDataAccess;

    public async Task MigrateToGraphDatabaseAsync()
    {
        var schema = await _relationalSchemaExtractor.GetSchema();

        var (entityTables, relationshipTables) = ClassifyTables(schema);

        await using var sqlConnection = new SqlConnection(configurationOptions.Value.ConnectionString);
        await sqlConnection.OpenAsync();

        foreach (var table in entityTables)
        {
            await CreateNodesForTableAsync(sqlConnection, table);
        }

        foreach (var table in relationshipTables)
        {
            await CreateRelationshipsFromLinkingTableAsync(sqlConnection, table);
        }

        foreach (var table in entityTables)
        {
            foreach (var foreignKey in table.ForeignKeys)
            {
                await CreateAdditionalRelationshipsAsync(sqlConnection, table, foreignKey);
            }
        }
    }

    private async Task CreateNodesForTableAsync(SqlConnection sqlConnection, TableSchema table)
    {
        var columnsToInclude = table.Columns
            .Where(c => !table.ForeignKeys
                              .Any(fk => fk.ColumnName == c.Name) || table.PrimaryKeys.Contains(c.Name))
            .Select(c => c.Name);

        var columnsStr = string.Join(", ", columnsToInclude);
        var query = $"SELECT {columnsStr} FROM [{table.Name}]";

        using var command = new SqlCommand(query, sqlConnection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var properties = new Dictionary<string, object>();
            foreach (var column in columnsToInclude)
            {
                var value = reader[column];
                if (value != DBNull.Value)
                {
                    properties[column] = GetValue(value);
                }

            }

            var cypher = $@"CREATE (n:{table.Name} $props) RETURN true";

            await _neo4JDataAccess.ExecuteWriteTransactionAsync<bool>(cypher, new
            {
                props = properties
            });
        }
    }

    private async Task CreateRelationshipsFromLinkingTableAsync(SqlConnection sqlConnection, TableSchema table)
    {
        var nonForeignKeyColumns = table.Columns
            .Where(c => !table.ForeignKeys.Any(fk => fk.ColumnName == c.Name))
            .Select(c => c.Name);

        var columnsStr = string.Join(", ", table.Columns.Select(c => c.Name));
        var query = $"SELECT {columnsStr} FROM [{table.Name}]";

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

            await _neo4JDataAccess.ExecuteWriteTransactionAsync<bool>(cypher, new
            {
                sourceId = GetValue(reader[fk1.ColumnName]),
                targetId = GetValue(reader[fk2.ColumnName]),
                props = properties
            });
        }
    }

    private async Task CreateAdditionalRelationshipsAsync(SqlConnection sqlConnection, TableSchema table, ForeignKeySchema fk)
    {
        var primaryIdColumnName = table.PrimaryKeys[0];
        var query = $"SELECT {primaryIdColumnName}, {fk.ColumnName} FROM [{table.Name}]";

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

            await _neo4JDataAccess.ExecuteWriteTransactionAsync<bool>(cypher, new
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
            if (IsEntityTable(table))
            {
                entityTables.Add(table);
            }
            else if (IsRelationshipTable(table))
            {
                relationshipTables.Add(table);
            }
            else
            {
                entityTables.Add(table);
            }
        }

        return (entityTables, relationshipTables);
    }

    private static bool IsEntityTable(TableSchema table)
    {
        return
            table.ForeignKeys.Count == 0 ||
            table.ForeignKeys.Count == 1 || table.ForeignKeys.Count > 2 ||
            (table.ForeignKeys.Count == 2 && table.PrimaryKeys.Count == 1);
    }

    private static bool IsRelationshipTable(TableSchema table)
    {
        if (table.ForeignKeys.Count != 2)
            return false;

        return
            table.PrimaryKeys.Count != 1 ||
            table.PrimaryKeys.All(pk => table.ForeignKeys.Any(fk => fk.ColumnName == pk));
    }

    private static object GetValue(object value)
    {
        return value is Guid ? value.ToString() : value;
    }
}
