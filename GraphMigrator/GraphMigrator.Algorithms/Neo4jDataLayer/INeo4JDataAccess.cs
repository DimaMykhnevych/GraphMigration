﻿namespace GraphMigrator.Algorithms.Neo4jDataLayer;

public interface INeo4jDataAccess : IAsyncDisposable
{
    Task<List<string>> ExecuteReadListAsync(string query, string returnObjectKey, IDictionary<string, object> parameters = null);

    Task<List<Dictionary<string, object>>> ExecuteReadDictionaryAsync(string query, IDictionary<string, object> parameters = null);

    Task<T> ExecuteReadScalarAsync<T>(string query, IDictionary<string, object> parameters = null);

    Task<T> ExecuteWriteTransactionAsync<T>(string query, object parameters = null);

    Task ExecuteWriteTransactionAsync(string query, object parameters = null);

    Task DeleteSchemaWithData();
}
