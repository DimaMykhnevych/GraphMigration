using GraphMigrator.Domain.Configuration;
using GraphMigrator.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace GraphMigrator.Algorithms.RelationalSchemaExtractors;

public class MSSQLExtractor(IOptions<SourceDataSourceConfiguration> configurationOptions) : IRelationalSchemaExtractor
{
    private readonly SourceDataSourceConfiguration configuration = configurationOptions.Value;

    private readonly List<string> tablesToExclude = ["sysdiagrams"];

    public async Task<RelationalDatabaseSchema> GetSchema()
    {
        var databaseSchema = new RelationalDatabaseSchema();
        using SqlConnection connection = new(configuration.ConnectionString);

        try
        {
            await connection.OpenAsync();
            var tables = await GetTables(connection);

            if (tables.Count(t => !tablesToExclude.Contains(t)) >= 10)
            {
                await PopulateSchemaInfoParallel(databaseSchema, tables);
            }
            else
            {
                await PopulateSchemaInfoSequential(connection, databaseSchema, tables);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }


        return databaseSchema;
    }

    private async Task PopulateSchemaInfoSequential(SqlConnection connection, RelationalDatabaseSchema databaseSchema, List<string> tables)
    {
        foreach (var tableName in tables)
        {
            if (tablesToExclude.Contains(tableName))
            {
                continue;
            }

            var tableSchema = new TableSchema
            {
                Name = tableName,
                Columns = await GetColumnsAsync(connection, tableName),
                PrimaryKeys = await GetPrimaryKeysAsync(connection, tableName),
                ForeignKeys = await GetForeignKeysAsync(connection, tableName)
            };

            databaseSchema.Tables.Add(tableSchema);
        }
    }

    private async Task PopulateSchemaInfoParallel(RelationalDatabaseSchema databaseSchema, List<string> tables)
    {
        var tableSchemas = new ConcurrentBag<TableSchema>();

        await Parallel.ForEachAsync(tables,
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 },
            async (tableName, ct) =>
            {
                if (tablesToExclude.Contains(tableName))
                {
                    return;
                }

                using var connection = new SqlConnection(configuration.ConnectionString);
                await connection.OpenAsync(ct);

                var tableSchema = new TableSchema
                {
                    Name = tableName,
                    Columns = await GetColumnsAsync(connection, tableName),
                    PrimaryKeys = await GetPrimaryKeysAsync(connection, tableName),
                    ForeignKeys = await GetForeignKeysAsync(connection, tableName)
                };
                tableSchemas.Add(tableSchema);
            });

        databaseSchema.Tables = tableSchemas
            .OrderBy(t => t.Name)
            .ToList();
    }

    private static async Task<List<string>> GetTables(SqlConnection connection)
    {
        List<string> tables = [];

        const string query = @"
            SELECT TABLE_NAME 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_TYPE = 'BASE TABLE'";

        using SqlCommand command = new(query, connection);
        using SqlDataReader reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tables.Add(reader["TABLE_NAME"].ToString());
        }

        return tables;
    }

    private static async Task<List<ColumnSchema>> GetColumnsAsync(SqlConnection connection, string tableName)
    {
        var columns = new List<ColumnSchema>();

        const string query = @"
            SELECT 
                COLUMN_NAME,
                DATA_TYPE,
                CHARACTER_MAXIMUM_LENGTH,
                IS_NULLABLE,
                COLUMN_DEFAULT
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @TableName
            ORDER BY ORDINAL_POSITION";

        using SqlCommand command = new(query, connection);

        command.Parameters.AddWithValue("@TableName", tableName);

        using SqlDataReader reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var column = new ColumnSchema
            {
                Name = reader["COLUMN_NAME"].ToString(),
                DataType = reader["DATA_TYPE"].ToString(),
                MaxLength = reader["CHARACTER_MAXIMUM_LENGTH"] != DBNull.Value
                    ? Convert.ToInt32(reader["CHARACTER_MAXIMUM_LENGTH"])
                    : null,
                IsNullable = reader["IS_NULLABLE"].ToString() == "YES",
                DefaultValue = reader["COLUMN_DEFAULT"].ToString()
            };

            columns.Add(column);
        }

        return columns;
    }

    private static async Task<List<string>> GetPrimaryKeysAsync(SqlConnection connection, string tableName)
    {
        var primaryKeys = new List<string>();

        const string query = @"
            SELECT 
                Col.COLUMN_NAME
            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS Tab
            JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE Col 
                ON Col.CONSTRAINT_NAME = Tab.CONSTRAINT_NAME
            WHERE Tab.CONSTRAINT_TYPE = 'PRIMARY KEY'
                AND Tab.TABLE_NAME = @TableName";

        using SqlCommand command = new(query, connection);

        command.Parameters.AddWithValue("@TableName", tableName);

        using SqlDataReader reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            primaryKeys.Add(reader["COLUMN_NAME"].ToString());
        }

        return primaryKeys;
    }

    private static async Task<List<ForeignKeySchema>> GetForeignKeysAsync(SqlConnection connection, string tableName)
    {
        var foreignKeys = new List<ForeignKeySchema>();

        const string query = @"
            SELECT 
                FK.name AS FKName,
                ParentSchema = SCHEMA_NAME(ParentTable.schema_id),
                ParentTable = ParentTable.name,
                ParentColumn = ParentCol.name,
                ReferencedSchema = SCHEMA_NAME(ReferencedTable.schema_id),
                ReferencedTable = ReferencedTable.name,
                ReferencedColumn = ReferencedCol.name
            FROM sys.foreign_keys FK
            INNER JOIN sys.tables ParentTable 
                ON FK.parent_object_id = ParentTable.object_id
            INNER JOIN sys.tables ReferencedTable 
                ON FK.referenced_object_id = ReferencedTable.object_id
            INNER JOIN sys.foreign_key_columns FKCols
                ON FK.object_id = FKCols.constraint_object_id
            INNER JOIN sys.columns ParentCol
                ON FKCols.parent_column_id = ParentCol.column_id
                AND FKCols.parent_object_id = ParentCol.object_id
            INNER JOIN sys.columns ReferencedCol
                ON FKCols.referenced_column_id = ReferencedCol.column_id
                AND FKCols.referenced_object_id = ReferencedCol.object_id
            WHERE ParentTable.name = @TableName";

        using SqlCommand command = new(query, connection);

        command.Parameters.AddWithValue("@TableName", tableName);

        using SqlDataReader reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var foreignKey = new ForeignKeySchema
            {
                Name = reader["FKName"].ToString(),
                ColumnName = reader["ParentColumn"].ToString(),
                ReferencedTableSchema = reader["ReferencedSchema"].ToString(),
                ReferencedTableName = reader["ReferencedTable"].ToString(),
                ReferencedColumnName = reader["ReferencedColumn"].ToString()
            };

            foreignKeys.Add(foreignKey);
        }

        return foreignKeys;
    }
}
