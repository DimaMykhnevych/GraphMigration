﻿using GraphMigrator.Algorithms.Neo4jDataLayer;
using GraphMigrator.Algorithms.RelationalSchemaExtractors;
using GraphMigrator.Domain.Configuration;
using GraphMigrator.Domain.Entities;
using GraphMigrator.Domain.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GraphMigrator.Algorithms.Rel2GraphParallel;

public class Rel2GraphParallelAlgorithm : IRel2GraphParallelAlgorithm
{
    private readonly IRelationalSchemaExtractor _relationalSchemaExtractor;
    private readonly SourceDataSourceConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly SemaphoreSlim _dbConnectionSemaphore;
    private readonly SemaphoreSlim _neo4jWriteSemaphore;
    private readonly int _maxConcurrentDbConnections;
    private readonly int _maxConcurrentNeo4jWrites;
    private readonly int _batchSize;

    public Rel2GraphParallelAlgorithm(
        IRelationalSchemaExtractor relationalSchemaExtractor,
        IOptions<SourceDataSourceConfiguration> configurationOptions,
        IServiceProvider serviceProvider)
    {
        _relationalSchemaExtractor = relationalSchemaExtractor;
        _configuration = configurationOptions.Value;
        _serviceProvider = serviceProvider;

        _maxConcurrentDbConnections = Environment.ProcessorCount * 2;
        _maxConcurrentNeo4jWrites = Environment.ProcessorCount * 4;
        _batchSize = 1000;

        _dbConnectionSemaphore = new SemaphoreSlim(_maxConcurrentDbConnections, _maxConcurrentDbConnections);
        _neo4jWriteSemaphore = new SemaphoreSlim(_maxConcurrentNeo4jWrites, _maxConcurrentNeo4jWrites);
    }

    public async Task MigrateToGraphDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var schema = await _relationalSchemaExtractor.GetSchema();
        var (entityTables, relationshipTables) = ClassifyTables(schema);

        await ProcessTablesInParallel(entityTables, CreateNodesForTableAsync, cancellationToken);

        await ProcessTablesInParallel(relationshipTables, CreateRelationshipsFromLinkingTableAsync, cancellationToken);

        var foreignKeyPairs = entityTables
            .SelectMany(table => table.ForeignKeys.Select(fk => new ForeignKeyPair
            {
                Table = table,
                ForeignKey = fk
            }))
        .ToList();

        await ProcessForeignKeysInParallel(foreignKeyPairs, cancellationToken);
    }

    private async Task ProcessTablesInParallel<T>(
        List<T> tables,
        Func<T, CancellationToken, Task> processor,
        CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        var partitionCount = _maxConcurrentDbConnections;
        var partitionSize = (int)Math.Ceiling(tables.Count / (double)partitionCount);

        for (int i = 0; i < partitionCount; i++)
        {
            var startIndex = i * partitionSize;
            var count = Math.Min(partitionSize, tables.Count - startIndex);

            if (count <= 0) continue;

            var partition = tables.GetRange(startIndex, count);

            tasks.Add(Task.Run(async () =>
            {
                foreach (var item in partition)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await _dbConnectionSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        await processor(item, cancellationToken);
                    }
                    finally
                    {
                        _dbConnectionSemaphore.Release();
                    }
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task ProcessForeignKeysInParallel(
        List<ForeignKeyPair> foreignKeyPairs,
        CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        var partitionCount = _maxConcurrentDbConnections;
        var partitionSize = (int)Math.Ceiling(foreignKeyPairs.Count / (double)partitionCount);

        for (int i = 0; i < partitionCount; i++)
        {
            var startIndex = i * partitionSize;
            var count = Math.Min(partitionSize, foreignKeyPairs.Count - startIndex);

            if (count <= 0) continue;

            var partition = foreignKeyPairs.GetRange(startIndex, count);

            tasks.Add(Task.Run(async () =>
            {
                foreach (var pair in partition)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    await _dbConnectionSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        await CreateAdditionalRelationshipsAsync(pair.Table, pair.ForeignKey, cancellationToken);
                    }
                    finally
                    {
                        _dbConnectionSemaphore.Release();
                    }
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    private async Task CreateNodesForTableAsync(TableSchema table, CancellationToken cancellationToken)
    {
        var columnsToInclude = table.Columns
            .Where(c => !table.ForeignKeys
                            .Any(fk => fk.ColumnName == c.Name) || table.PrimaryKeys.Contains(c.Name))
            .Select(c => c.Name)
            .ToList();

        var columnsStr = string.Join(", ", columnsToInclude);
        var query = $"SELECT {columnsStr} FROM [{table.Name}]";

        try
        {
            await using var sqlConnection = new SqlConnection(_configuration.ConnectionString);
            await sqlConnection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(query, sqlConnection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var batchCount = 0;
            var batchItems = new List<Dictionary<string, object>>(_batchSize);

            while (await reader.ReadAsync(cancellationToken))
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

                batchItems.Add(properties);
                batchCount++;

                if (batchCount >= _batchSize)
                {
                    await ProcessNodeBatchAsync(table.Name, batchItems, cancellationToken);
                    batchItems.Clear();
                    batchCount = 0;
                }
            }

            if (batchItems.Count > 0)
            {
                await ProcessNodeBatchAsync(table.Name, batchItems, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task ProcessNodeBatchAsync(
        string tableName,
        List<Dictionary<string, object>> batchItems,
        CancellationToken cancellationToken)
    {
        try
        {
            await _neo4jWriteSemaphore.WaitAsync(cancellationToken);

            var neo4JDataAccess = _serviceProvider.GetRequiredService<INeo4jDataAccess>();

            var cypher = $@"
                UNWIND $items AS item
                CREATE (n:{tableName}) 
                SET n = item
                RETURN count(n)";

            await neo4JDataAccess.ExecuteWriteTransactionAsync<int>(cypher, new
            {
                items = batchItems
            });
        }
        finally
        {
            _neo4jWriteSemaphore.Release();
        }
    }

    private async Task CreateRelationshipsFromLinkingTableAsync(TableSchema table, CancellationToken cancellationToken)
    {
        var nonForeignKeyColumns = table.Columns
            .Where(c => !table.ForeignKeys.Any(fk => fk.ColumnName == c.Name))
            .Select(c => c.Name)
            .ToList();

        var columnsStr = string.Join(", ", table.Columns.Select(c => c.Name));
        var query = $"SELECT {columnsStr} FROM [{table.Name}]";

        try
        {
            await using var sqlConnection = new SqlConnection(_configuration.ConnectionString);
            await sqlConnection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(query, sqlConnection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var batchCount = 0;
            var batchItems = new List<(object SourceId, object TargetId, Dictionary<string, object> Properties)>(_batchSize);

            var fk1 = table.ForeignKeys[0];
            var fk2 = table.ForeignKeys[1];

            while (await reader.ReadAsync(cancellationToken))
            {
                var properties = new Dictionary<string, object>();
                foreach (var column in nonForeignKeyColumns)
                {
                    var value = reader[column];
                    if (value != DBNull.Value)
                        properties[column] = GetValue(value);
                }

                var sourceId = GetValue(reader[fk1.ColumnName]);
                var targetId = GetValue(reader[fk2.ColumnName]);

                batchItems.Add((sourceId, targetId, properties));
                batchCount++;

                if (batchCount >= _batchSize)
                {
                    await ProcessRelationshipBatchAsync(table.Name, fk1, fk2, batchItems, cancellationToken);
                    batchItems.Clear();
                    batchCount = 0;
                }
            }

            if (batchItems.Count > 0)
            {
                await ProcessRelationshipBatchAsync(table.Name, fk1, fk2, batchItems, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task ProcessRelationshipBatchAsync(
        string relationshipType,
        ForeignKeySchema fk1,
        ForeignKeySchema fk2,
        List<(object SourceId, object TargetId, Dictionary<string, object> Properties)> batchItems,
        CancellationToken cancellationToken)
    {
        try
        {
            await _neo4jWriteSemaphore.WaitAsync(cancellationToken);

            var neo4JDataAccess = _serviceProvider.GetRequiredService<INeo4jDataAccess>();

            var cypher = $@"
                UNWIND $items AS item
                MATCH (source:{fk1.ReferencedTableName} {{{fk1.ReferencedColumnName}: item.sourceId}})
                MATCH (target:{fk2.ReferencedTableName} {{{fk2.ReferencedColumnName}: item.targetId}})
                CREATE (source)-[r:{relationshipType}]->(target)
                SET r = item.props
                RETURN count(r)";

            var parameters = new
            {
                items = batchItems.Select(item => new
                {
                    sourceId = item.SourceId,
                    targetId = item.TargetId,
                    props = item.Properties
                }).ToList()
            };

            await neo4JDataAccess.ExecuteWriteTransactionAsync<int>(cypher, parameters);
        }
        finally
        {
            _neo4jWriteSemaphore.Release();
        }
    }

    private async Task CreateAdditionalRelationshipsAsync(
        TableSchema table,
        ForeignKeySchema fk,
        CancellationToken cancellationToken)
    {
        var primaryIdColumnName = table.PrimaryKeys[0];
        var query = $"SELECT {primaryIdColumnName}, {fk.ColumnName} FROM [{table.Name}]";

        try
        {
            await using var sqlConnection = new SqlConnection(_configuration.ConnectionString);
            await sqlConnection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(query, sqlConnection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var batchCount = 0;
            var batchItems = new List<(object SourceId, object TargetId)>(_batchSize);

            while (await reader.ReadAsync(cancellationToken))
            {
                var sourceId = GetValue(reader[primaryIdColumnName]);
                var targetId = GetValue(reader[fk.ColumnName]);

                if (targetId != null)
                {
                    batchItems.Add((sourceId, targetId));
                    batchCount++;

                    if (batchCount >= _batchSize)
                    {
                        await ProcessForeignKeyBatchAsync(table.Name, fk, primaryIdColumnName, batchItems, cancellationToken);
                        batchItems.Clear();
                        batchCount = 0;
                    }
                }
            }

            if (batchItems.Count > 0)
            {
                await ProcessForeignKeyBatchAsync(table.Name, fk, primaryIdColumnName, batchItems, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task ProcessForeignKeyBatchAsync(
        string tableName,
        ForeignKeySchema fk,
        string primaryIdColumnName,
        List<(object SourceId, object TargetId)> batchItems,
        CancellationToken cancellationToken)
    {
        try
        {
            await _neo4jWriteSemaphore.WaitAsync(cancellationToken);

            var neo4JDataAccess = _serviceProvider.GetRequiredService<INeo4jDataAccess>();

            var relationshipType = $"HAS_{fk.ReferencedTableName}_{tableName}";

            var cypher = $@"
                UNWIND $items AS item
                MATCH (source:{tableName} {{{primaryIdColumnName}: item.sourceId}})
                MATCH (target:{fk.ReferencedTableName} {{{fk.ReferencedColumnName}: item.targetId}})
                CREATE (target)-[r:{relationshipType}]->(source)
                RETURN count(r)";

            var parameters = new
            {
                items = batchItems.Select(item => new
                {
                    sourceId = item.SourceId,
                    targetId = item.TargetId
                }).ToList()
            };

            await neo4JDataAccess.ExecuteWriteTransactionAsync<int>(cypher, parameters);
        }
        finally
        {
            _neo4jWriteSemaphore.Release();
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

    public void Dispose()
    {
        _dbConnectionSemaphore?.Dispose();
        _neo4jWriteSemaphore?.Dispose();
    }
}