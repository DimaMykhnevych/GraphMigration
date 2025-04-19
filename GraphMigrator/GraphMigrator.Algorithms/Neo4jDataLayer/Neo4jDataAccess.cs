using Neo4j.Driver;

namespace GraphMigrator.Algorithms.Neo4jDataLayer;

public class Neo4jDataAccess : INeo4jDataAccess
{
    private readonly IAsyncSession _session;

    private readonly string _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="Neo4jDataAccess"/> class.
    /// </summary>
    public Neo4jDataAccess(IDriver driver, string database)
    {
        _database = database ?? "neo4j";
        _session = driver.AsyncSession(o => o.WithDatabase(_database));
    }

    /// <summary>
    /// Execute read list as an asynchronous operation.
    /// </summary>
    public async Task<List<string>> ExecuteReadListAsync(string query, string returnObjectKey, IDictionary<string, object> parameters = null)
    {
        return await ExecuteReadTransactionAsync<string>(query, returnObjectKey, parameters);
    }

    /// <summary>
    /// Execute read dictionary as an asynchronous operation.
    /// </summary>
    public async Task<List<Dictionary<string, object>>> ExecuteReadDictionaryAsync(string query, IDictionary<string, object> parameters = null)
    {
        return await ExecuteReadTransactionAsync(query, parameters);
    }

    /// <summary>
    /// Execute read scalar as an asynchronous operation.
    /// </summary>
    public async Task<T> ExecuteReadScalarAsync<T>(string query, IDictionary<string, object> parameters = null)
    {
        try
        {
            parameters = parameters == null ? new Dictionary<string, object>() : parameters;

            var result = await _session.ExecuteReadAsync(async tx =>
            {
                T scalar = default;

                var res = await tx.RunAsync(query, parameters);

                scalar = (await res.SingleAsync())[0].As<T>();

                return scalar;
            });

            return result;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Execute write transaction
    /// </summary>
    public async Task<T> ExecuteWriteTransactionAsync<T>(string query, object parameters = null)
    {
        try
        {
            parameters = parameters == null ? new Dictionary<string, object>() : parameters;

            var result = await _session.ExecuteWriteAsync(async tx =>
            {
                T scalar = default;

                var res = await tx.RunAsync(query, parameters);

                scalar = (await res.SingleAsync())[0].As<T>();

                return scalar;
            });

            return result;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Execute write transaction without result.
    /// </summary>
    public async Task ExecuteWriteTransactionAsync(string query, object parameters = null)
    {
        try
        {
            parameters = parameters == null ? new Dictionary<string, object>() : parameters;

            await _session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(query, parameters);
            });
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task DeleteSchemaWithData()
    {
        var cypherQuery = @"
            MATCH (n)
            WITH n LIMIT 1000
            DETACH DELETE n
            RETURN COUNT(n) AS deletedCount";
        try
        {
            bool hasMoreNodes;
            do
            {
                var result = await _session.ExecuteWriteAsync(async tx =>
                {
                    var cursor = await tx.RunAsync(cypherQuery);
                    var record = await cursor.SingleAsync();
                    return record["deletedCount"].As<int>();
                });

                hasMoreNodes = result > 0;
            }
            while (hasMoreNodes);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Execute read transaction as an asynchronous operation.
    /// </summary>
    private async Task<List<T>> ExecuteReadTransactionAsync<T>(string query, string returnObjectKey, IDictionary<string, object> parameters)
    {
        try
        {
            parameters = parameters == null ? new Dictionary<string, object>() : parameters;

            var result = await _session.ExecuteReadAsync(async tx =>
            {
                var data = new List<T>();

                var res = await tx.RunAsync(query, parameters);

                var records = await res.ToListAsync();

                data = records.Select(x => (T)x.Values[returnObjectKey]).ToList();

                return data;
            });

            return result;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task<List<Dictionary<string, object>>> ExecuteReadTransactionAsync(string query, IDictionary<string, object> parameters)
    {
        try
        {
            parameters = parameters ?? new Dictionary<string, object>();

            var result = await _session.ExecuteReadAsync(async tx =>
            {
                var data = new List<Dictionary<string, object>>();

                var res = await tx.RunAsync(query, parameters);

                var records = await res.ToListAsync();

                data = records.Select(record => record.Values.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                )).ToList();

                return data;
            });

            return result;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or
    /// resetting unmanaged resources asynchronously.
    /// </summary>
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await _session.CloseAsync();
    }
}
