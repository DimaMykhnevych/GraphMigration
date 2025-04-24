namespace GraphMigrator.Domain.Models;

public class Neo4jPerformanceMetrics
{
    public long ExecutionTimeMs { get; set; }
    public long MemoryUsageBytes { get; set; }
    public double CpuPercentage { get; set; }
    public long DatabaseHits { get; set; }
    public Dictionary<string, object> ProfileInfo { get; set; }
}

