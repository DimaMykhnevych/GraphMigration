using GraphMigrator.Domain.Models;

namespace GraphMigrator.Algorithms.QueryComparison;

public interface IEnhancedQueryComparisonAlgorithm
{
    Task<EnhancedQueryComparisonResult> CompareQueryResultsWithPerformanceMetrics(
        string sqlQuery,
        string cypherQuery,
        string targetDatabaseName,
        int? fractionalDigitsNumber = null,
        int? resultsCountToReturn = null);
}