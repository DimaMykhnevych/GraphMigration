namespace GraphMigrator.Domain.Models;

public class SqlPerformanceMetrics
{
    public long ExecutionTimeMs { get; set; }
    public long MemoryUsageBytes { get; set; }
    public double CpuPercentage { get; set; }
    public int DatabaseHits { get; set; }
}
