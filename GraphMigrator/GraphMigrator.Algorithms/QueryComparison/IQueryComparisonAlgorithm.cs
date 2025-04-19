using GraphMigrator.Domain.Models;

namespace GraphMigrator.Algorithms.QueryComparison;

public interface IQueryComparisonAlgorithm
{
    Task<QueryComparisonResult> CompareQueryResults(
        string sqlQuery,
        string cypherQuery,
        string targetDatabaseName,
        int? fractionalDigitsNumber = null,
        int? resultsCountToReturn = null);
}

