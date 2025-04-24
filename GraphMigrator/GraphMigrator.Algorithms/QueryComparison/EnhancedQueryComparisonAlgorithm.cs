using GraphMigrator.Domain.Configuration;
using GraphMigrator.Domain.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using System.Data;
using System.Diagnostics;

namespace GraphMigrator.Algorithms.QueryComparison;

public class EnhancedQueryComparisonAlgorithm : IEnhancedQueryComparisonAlgorithm
{
    private readonly SourceDataSourceConfiguration _configuration;
    private readonly IDriver _neo4JDriver;
    private readonly IQueryComparisonAlgorithm _baseAlgorithm;

    public EnhancedQueryComparisonAlgorithm(
        IOptions<SourceDataSourceConfiguration> configurationOptions,
        IDriver neo4JDriver,
        IQueryComparisonAlgorithm baseAlgorithm)
    {
        _configuration = configurationOptions.Value;
        _neo4JDriver = neo4JDriver;
        _baseAlgorithm = baseAlgorithm;
    }

    public async Task<EnhancedQueryComparisonResult> CompareQueryResultsWithPerformanceMetrics(
        string sqlQuery,
        string cypherQuery,
        string targetDatabaseName,
        int? fractionalDigitsNumber = null,
        int? resultsCountToReturn = null)
    {
        var baseResult = await _baseAlgorithm.CompareQueryResults(
            sqlQuery,
            cypherQuery,
            targetDatabaseName,
            fractionalDigitsNumber,
            resultsCountToReturn);

        var sqlMetrics = await GetSqlPerformanceMetrics(sqlQuery);

        var neo4jMetrics = await GetNeo4jPerformanceMetrics(cypherQuery, targetDatabaseName);

        var enhancedResult = new EnhancedQueryComparisonResult
        {
            AreIdentical = baseResult.AreIdentical,
            Differences = baseResult.Differences,
            SqlResults = baseResult.SqlResults,
            CypherResults = baseResult.CypherResults,
            SqlPerformanceMetrics = sqlMetrics,
            Neo4jPerformanceMetrics = neo4jMetrics
        };

        return enhancedResult;
    }

    private async Task<SqlPerformanceMetrics> GetSqlPerformanceMetrics(string query)
    {
        var metrics = new SqlPerformanceMetrics();
        var process = Process.GetCurrentProcess();

        using var cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);
        cpuCounter.NextValue();

        using var connection = new SqlConnection(_configuration.ConnectionString);
        await connection.OpenAsync();

        using (var statCommand = new SqlCommand("SET STATISTICS IO ON; SET STATISTICS TIME ON;", connection))
        {
            await statCommand.ExecuteNonQueryAsync();
        }

        string ioStatsQuery = @"
        SELECT 
            s.total_logical_reads AS total_logical_reads,
            s.total_physical_reads AS total_physical_reads,
            s.last_used_grant_kb AS last_used_grant_kb,
            s.last_elapsed_time AS last_elapsed_time
        FROM sys.dm_exec_query_stats AS s
        CROSS APPLY sys.dm_exec_sql_text(s.sql_handle) AS t
        WHERE t.text LIKE @QueryPattern
        AND s.last_execution_time > DATEADD(SECOND, -30, GETDATE())";

        using var command = new SqlCommand(query, connection);
        command.CommandTimeout = 300;

        using var reader = await command.ExecuteReaderAsync();
        var dataTable = new DataTable();
        dataTable.Load(reader);

        try
        {
            using var statsCommand = new SqlCommand(ioStatsQuery, connection);
            var replacedQuery = query.Split("\n").First().Replace("'", "''");
            statsCommand.Parameters.AddWithValue("@QueryPattern", $"%{replacedQuery}%");

            using var statsReader = await statsCommand.ExecuteReaderAsync();
            if (await statsReader.ReadAsync())
            {
                var logicalReads = statsReader["total_logical_reads"] != DBNull.Value ?
                    Convert.ToInt32(statsReader["total_logical_reads"]) : 0;

                var physicalReads = statsReader["total_physical_reads"] != DBNull.Value ?
                    Convert.ToInt32(statsReader["total_physical_reads"]) : 0;

                metrics.DatabaseHits = logicalReads + physicalReads;

                var kbMemory = statsReader["last_used_grant_kb"] != DBNull.Value ?
                    Convert.ToInt32(statsReader["last_used_grant_kb"]) : 0;
                metrics.MemoryUsageBytes = kbMemory * 1000;

                var timeInMicroseconds = statsReader["last_elapsed_time"] != DBNull.Value ?
                    Convert.ToInt32(statsReader["last_elapsed_time"]) : 0;

                metrics.ExecutionTimeMs = timeInMicroseconds / 1000;
            }
        }
        catch (SqlException ex)
        {
            // Fallback for when the DMV query fails - often due to permissions
            metrics.DatabaseHits = -1; // Indicates we couldn't get this data

            // Alternative: Try using SET STATISTICS IO to parse output, but this is complex to implement
            // For now, we'll just capture that we couldn't get the data
        }

        // Calculate CPU usage
        double cpuPercentage = cpuCounter.NextValue() / Environment.ProcessorCount;

        // Calculate metrics
        metrics.CpuPercentage = Math.Round(cpuPercentage, 2);

        // If we couldn't get DB hits from DMVs, try to get via another approach
        if (metrics.DatabaseHits == -1)
        {
            // Try alternative approach: use SET STATISTICS IO parser or just execute a simpler query
            using var simpleStatsCommand = new SqlCommand(@"
                SELECT 
                    @@CPU_BUSY AS cpu_busy,
                    @@IO_BUSY AS io_busy,
                    @@TOTAL_READ AS total_reads,
                    @@TOTAL_WRITE AS total_writes
                ", connection);

            using var simpleStatsReader = await simpleStatsCommand.ExecuteReaderAsync();
            if (await simpleStatsReader.ReadAsync())
            {
                metrics.DatabaseHits = Convert.ToInt32(simpleStatsReader["total_reads"]) +
                                      Convert.ToInt32(simpleStatsReader["total_writes"]);

                // Note: This is system-wide, not query-specific, so it's less accurate but better than nothing
            }
        }

        try
        {
            //using var cleanStatistics = new SqlCommand("DBCC FREEPROCCACHE; DBCC DROPCLEANBUFFERS", connection);
            //cleanStatistics.CommandTimeout = 300;

            //await cleanStatistics.ExecuteReaderAsync();
        }
        catch (Exception)
        {
            // Handle exception
        }

        return metrics;
    }

    private async Task<Neo4jPerformanceMetrics> GetNeo4jPerformanceMetrics(string query, string targetDatabaseName)
    {
        var metrics = new Neo4jPerformanceMetrics();

        var session = _neo4JDriver.AsyncSession(o => o.WithDatabase(targetDatabaseName));
        try
        {
            var process = Process.GetCurrentProcess();

            using var cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);
            cpuCounter.NextValue();

            // Add PROFILE keyword to the Cypher query to get execution statistics
            string profiledQuery = query.Trim().StartsWith("PROFILE ", StringComparison.OrdinalIgnoreCase) ?
                query : "PROFILE " + query;

            var result = await session.RunAsync(profiledQuery);
            var records = await result.ToListAsync();
            var summary = await result.ConsumeAsync();

            // Extract profile information and database hits
            metrics.DatabaseHits = summary.Counters.NodesCreated +
                                summary.Counters.NodesDeleted +
                                summary.Counters.RelationshipsCreated +
                                summary.Counters.RelationshipsDeleted +
                                summary.Counters.PropertiesSet;

            metrics.DatabaseHits += GetDbHist(summary.Plan);

            // Extract execution time directly from the summary
            metrics.ExecutionTimeMs = (long)(summary.ResultAvailableAfter + summary.ResultConsumedAfter).TotalMilliseconds;

            // Extract memory usage from profile results if available
            var profileInfo = new Dictionary<string, object>();
            var plan = summary.Plan;
            if (plan != null)
            {
                // Extract memory information from the plan
                ExtractMemoryInfoFromPlan(plan, profileInfo);

                // Sum up memory usage across all operators
                long totalMemoryBytes = 0;
                foreach (var key in profileInfo.Keys.Where(k => k.Contains("memory") || k.Contains("Memory")))
                {
                    if (profileInfo[key] is long memBytes)
                    {
                        totalMemoryBytes += memBytes;
                    }
                }
                metrics.MemoryUsageBytes = totalMemoryBytes;
            }

            // Store additional Neo4j specific metrics
            metrics.ProfileInfo = new Dictionary<string, object>
        {
            { "NodesCreated", summary.Counters.NodesCreated },
            { "NodesDeleted", summary.Counters.NodesDeleted },
            { "RelationshipsCreated", summary.Counters.RelationshipsCreated },
            { "RelationshipsDeleted", summary.Counters.RelationshipsDeleted },
            { "PropertiesSet", summary.Counters.PropertiesSet },
            { "LabelsAdded", summary.Counters.LabelsAdded },
            { "LabelsRemoved", summary.Counters.LabelsRemoved },
            { "IndexesAdded", summary.Counters.IndexesAdded },
            { "IndexesRemoved", summary.Counters.IndexesRemoved },
            { "ConstraintsAdded", summary.Counters.ConstraintsAdded },
            { "ConstraintsRemoved", summary.Counters.ConstraintsRemoved },
            { "DbHits", metrics.DatabaseHits },
            { "ExecutionTimeMs", metrics.ExecutionTimeMs }
        };

            // For Neo4j, CPU usage is still calculated from process because Neo4j doesn't provide direct CPU metrics
            double cpuPercentage = cpuCounter.NextValue() / Environment.ProcessorCount;
            metrics.CpuPercentage = Math.Round(cpuPercentage, 2);
        }
        finally
        {
            await session.CloseAsync();
        }

        return metrics;
    }

    private void ExtractMemoryInfoFromPlan(IPlan plan, Dictionary<string, object> profileInfo)
    {
        if (plan == null) return;

        // Extract memory information from plan arguments
        foreach (var arg in plan.Arguments)
        {
            if (arg.Key.Contains("memory") || arg.Key.Contains("Memory"))
            {
                profileInfo[arg.Key] = arg.Value;
            }
        }

        // Recursively process child plans
        foreach (var child in plan.Children)
        {
            ExtractMemoryInfoFromPlan(child, profileInfo);
        }
    }

    private long GetDbHist(IPlan plan, long counter = 0)
    {
        var childrenCount = plan.Children.Count;
        if (childrenCount == 0)
        {
            return counter;
        }

        foreach (var child in plan.Children)
        {
            var profiledPlan = child as IProfiledPlan;
            counter += profiledPlan?.DbHits ?? 0;
            counter = GetDbHist(child, counter);
        }

        return counter;
    }
}
