namespace GraphMigrator.Domain.Models;

public class EnhancedQueryComparisonResult : QueryComparisonResult
{
    public SqlPerformanceMetrics SqlPerformanceMetrics { get; set; }
    public Neo4jPerformanceMetrics Neo4jPerformanceMetrics { get; set; }
}
