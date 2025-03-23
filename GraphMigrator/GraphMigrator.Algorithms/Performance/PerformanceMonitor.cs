using GraphMigrator.Domain.Models;
using System.Diagnostics;

namespace GraphMigrator.Algorithms.Performance;

public static class PerformanceMonitor
{
    public static async Task<PerformanceMetrics> MeasurePerformanceAsync(Func<Task> action)
    {
        var metrics = new PerformanceMetrics();
        var process = Process.GetCurrentProcess();

        // Initialize performance counters
        using var cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);
        using var ramCounter = new PerformanceCounter("Process", "Working Set - Private", process.ProcessName);

        // Take initial sample (first value is always 0)
        cpuCounter.NextValue();
        float initialRamMB = ramCounter.NextValue() / (1024 * 1024);

        // Start timing
        var stopwatch = Stopwatch.StartNew();

        // Run the algorithm
        await action();

        // Stop timing
        stopwatch.Stop();

        // Get final measurements
        double cpuPercentage = cpuCounter.NextValue() / Environment.ProcessorCount;
        float finalRamMB = ramCounter.NextValue() / (1024 * 1024);

        // Set metrics
        metrics.CpuUsagePercentage = Math.Round(cpuPercentage, 2);
        metrics.MemoryUsageMB = Math.Round(finalRamMB - initialRamMB, 2);
        metrics.ExecutionTime = stopwatch.Elapsed;

        return metrics;
    }
}
