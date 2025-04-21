using GraphMigrator.Algorithms.Neo4jDataLayer;
using GraphMigrator.Domain.Configuration;
using GraphMigrator.Domain.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using System.Data;

namespace GraphMigrator.Algorithms.QueryComparison;

public class QueryComparisonAlgorithm : IQueryComparisonAlgorithm
{
    private readonly SourceDataSourceConfiguration _configuration;
    private readonly IDriver _neo4JDriver;

    public QueryComparisonAlgorithm(IOptions<SourceDataSourceConfiguration> configurationOptions, IDriver neo4JDriver)
    {
        _configuration = configurationOptions.Value;
        _neo4JDriver = neo4JDriver;
    }

    public async Task<QueryComparisonResult> CompareQueryResults(
        string sqlQuery,
        string cypherQuery,
        string targetDatabaseName,
        int? fractionalDigitsNumber = null,
        int? resultsCountToReturn = null)
    {
        // Get SQL query results
        var sqlResults = await GetSqlResults(sqlQuery);

        // Get Neo4j query results
        var neo4jResults = await GetNeo4jResults(cypherQuery, targetDatabaseName);

        // Compare results
        var resultsToReturn = resultsCountToReturn == null
            ? sqlResults.Rows.Count
            : sqlResults.Rows.Count > resultsCountToReturn.Value
                ? resultsCountToReturn.Value
                : sqlResults.Rows.Count;
        var result = CompareResults(sqlResults, neo4jResults, fractionalDigitsNumber);
        result.CypherResults = neo4jResults.Take(resultsToReturn).ToList();
        result.SqlResults = ConvertDataTableToList(sqlResults, resultsToReturn);

        return result;
    }

    private async Task<DataTable> GetSqlResults(string query)
    {
        DataTable dataTable = new();

        using SqlConnection connection = new(_configuration.ConnectionString);

        await connection.OpenAsync();

        using SqlCommand command = new(query, connection);
        command.CommandTimeout = 300;

        using SqlDataAdapter adapter = new(command);
        adapter.Fill(dataTable);

        return dataTable;
    }

    private async Task<List<Dictionary<string, object>>> GetNeo4jResults(string query, string targetDatabaseName)
    {
        var neo4JDataAccess = new Neo4jDataAccess(_neo4JDriver, targetDatabaseName);
        return await neo4JDataAccess.ExecuteReadDictionaryAsync(query);
    }

    private static QueryComparisonResult CompareResults(DataTable sqlResults, IReadOnlyList<Dictionary<string, object>> neo4jResults, int? fractionalDigitsNumber)
    {
        var result = new QueryComparisonResult();

        if (sqlResults.Rows.Count != neo4jResults.Count)
        {
            result.AreIdentical = false;
            result.Differences.Add($"Row count differs: SQL has {sqlResults.Rows.Count} rows, Neo4j has {neo4jResults.Count} rows");

            if (Math.Abs(sqlResults.Rows.Count - neo4jResults.Count) > 100)
            {
                result.Differences.Add("Row count difference exceeds 100; detailed comparison skipped");
                return result;
            }
        }

        var sqlColumns = sqlResults.Columns.Cast<DataColumn>().Select(c => c.ColumnName.ToLower()).ToList();

        var neo4jColumns = neo4jResults.Any()
            ? neo4jResults[0].Keys.Select(k => k.ToLower()).ToList()
            : new List<string>();

        if (!sqlColumns.OrderBy(c => c).SequenceEqual(neo4jColumns.OrderBy(c => c)))
        {
            result.AreIdentical = false;

            var missingInSql = neo4jColumns.Except(sqlColumns);
            var missingInNeo4j = sqlColumns.Except(neo4jColumns);

            if (missingInSql.Any())
            {
                result.Differences.Add($"Columns missing in SQL: {string.Join(", ", missingInSql)}");
            }

            if (missingInNeo4j.Any())
            {
                result.Differences.Add($"Columns missing in Neo4j: {string.Join(", ", missingInNeo4j)}");
            }
        }

        var commonColumns = sqlColumns.Intersect(neo4jColumns).ToList();

        int maxRowsToCompare = Math.Min(sqlResults.Rows.Count, neo4jResults.Count);

        for (int i = 0; i < maxRowsToCompare; i++)
        {
            foreach (var column in commonColumns)
            {
                var sqlValue = sqlResults.Rows[i][column];

                var neo4jColumn = neo4jResults[i].Keys.FirstOrDefault(k => k.Equals(column, StringComparison.OrdinalIgnoreCase));
                var neo4jValue = neo4jResults[i][neo4jColumn];

                if (!AreValuesEqual(sqlValue, neo4jValue, fractionalDigitsNumber))
                {
                    result.AreIdentical = false;
                    result.Differences.Add($"Row {i + 1}, Column '{column}': SQL value '{sqlValue}' differs from Neo4j value '{neo4jValue}'");

                    if (result.Differences.Count >= 10)
                    {
                        result.Differences.Add("More than 10 differences found; additional differences not shown");
                        return result;
                    }
                }
            }
        }

        return result;
    }

    private static bool AreValuesEqual(object sqlValue, object neo4jValue, int? fractionalDigitsNumber)
    {
        if (sqlValue == DBNull.Value || sqlValue == null)
            return neo4jValue == null;

        if (neo4jValue == null)
            return sqlValue == DBNull.Value || sqlValue == null;

        if ((sqlValue is int || sqlValue is long || sqlValue is double || sqlValue is decimal || sqlValue is float) &&
            (neo4jValue is int || neo4jValue is long || neo4jValue is double || neo4jValue is decimal || neo4jValue is float))
        {
            if (fractionalDigitsNumber == null)
            {
                return Convert.ToDecimal(sqlValue) == Convert.ToDecimal(neo4jValue);
            }

            // Round values to the third decimal place before comparing
            decimal roundedSqlValue = Math.Round(Convert.ToDecimal(sqlValue), fractionalDigitsNumber.Value);
            decimal roundedNeo4jValue = Math.Round(Convert.ToDecimal(neo4jValue), fractionalDigitsNumber.Value);

            return roundedSqlValue == roundedNeo4jValue;
        }


        if ((sqlValue is DateTime sqlDateTime) && (neo4jValue is LocalDateTime neo4JLocal))
        {
            return sqlDateTime == neo4JLocal.ToDateTime();
        }

        return sqlValue.ToString().Equals(neo4jValue.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static List<Dictionary<string, object>> ConvertDataTableToList(DataTable dataTable, int resultsToReturn)
    {
        var result = new List<Dictionary<string, object>>();
        for (var i = 0; i < resultsToReturn; i++)
        {
            var row = dataTable.Rows[i];
            var rowDictionary = new Dictionary<string, object>();

            foreach (DataColumn column in dataTable.Columns)
            {
                rowDictionary[column.ColumnName] = row[column];
            }

            result.Add(rowDictionary);
        }

        return result;
    }

}

