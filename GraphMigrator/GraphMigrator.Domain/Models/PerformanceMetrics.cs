namespace GraphMigrator.Domain.Models;

public class PerformanceMetrics
{
    public double CpuUsagePercentage { get; set; }
    public double MemoryUsageMB { get; set; }
    public TimeSpan ExecutionTime { get; set; }
}
