using GraphMigrator;
using GraphMigrator.Algorithms.ImprovedMigrationAlgorithmN;
using GraphMigrator.Algorithms.Neo4jDataLayer;
using GraphMigrator.Algorithms.Performance;
using GraphMigrator.Algorithms.Rel2Graph;
using GraphMigrator.Algorithms.Rel2GraphParallel;
using GraphMigrator.Domain.Configuration;
using GraphMigrator.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using System.Reflection;

var configuration = CreateConfigurationBuilder(args).Build();
var serviceProvider = CreateIocContainer(configuration).BuildServiceProvider();

var neo4JDataDriver = serviceProvider.GetRequiredService<IDriver>();
var targetDatabaseNames = serviceProvider.GetRequiredService<IOptions<TargetDatbaseNames>>();

using var improvedAlgo = serviceProvider.GetRequiredService<IImprovedMigrationAlgorithm>();
var rel2graphAlgo = serviceProvider.GetRequiredService<IRel2GraphAlgorithm>();
using var rel2graphParallelAlgo = serviceProvider.GetRequiredService<IRel2GraphParallelAlgorithm>();

await Experiment1();

//var parallelRel2Graph = await Experiment2(rel2graphParallelAlgo.MigrateToGraphDatabaseAsync);
//Console.WriteLine($"\nREL2GRAPH parallel: Analysis completed in {parallelRel2Graph:F2} seconds");

//var improved = await Experiment2(improvedAlgo.MigrateToGraphDatabaseAsync);
//Console.WriteLine($"\nIMPROVED: Analysis completed in {improved:F2} seconds");

//async Task<double> Experiment2(Func<CancellationToken, Task> algo)
//{
//    var ct = new CancellationToken();
//    List<double> results = [];
//    for (int i = 0; i < 10; i++)
//    {
//        await dataAccess.DeleteSchemaWithData();
//        var startTime = DateTime.Now;
//        await algo(ct);
//        var duration = DateTime.Now - startTime;
//        results.Add(duration.TotalSeconds);
//    }

//    return results.Average();
//}

async Task Experiment1()
{
    Console.WriteLine($"\nRunning REL2GRAPH algorithm");
    await Experiment1Wrapper(rel2graphAlgo.MigrateToGraphDatabaseAsync, targetDatabaseNames.Value.Rel2Graph);

    Console.WriteLine($"\nRunning REL2GRAPH parallel algorithm");
    await Experiment1Wrapper(() => rel2graphParallelAlgo.MigrateToGraphDatabaseAsync(new CancellationToken()), targetDatabaseNames.Value.Rel2GraphParallel);

    Console.WriteLine($"\nRunning improved algorithm");
    await Experiment1Wrapper(() => improvedAlgo.MigrateToGraphDatabaseAsync(new CancellationToken()), targetDatabaseNames.Value.Improved);
}

async Task Experiment1Wrapper(Func<Task> algo, string databaseName)
{
    await CreateDatabase(databaseName);

    var metrics = await PerformanceMonitor.MeasurePerformanceAsync(() => algo());

    Console.WriteLine($"CPU Usage: {metrics.CpuUsagePercentage}%");
    Console.WriteLine($"Memory Usage: {metrics.MemoryUsageMB} MB");
    Console.WriteLine($"Execution Time: {metrics.ExecutionTime}");
}

async Task CreateDatabase(string databaseName)
{
    Neo4jDataAccess dataAccess = new(neo4JDataDriver, null); // <-- neo4J database data access
    await dataAccess.ExecuteWriteTransactionAsync($"DROP DATABASE {databaseName} if exists");
    await dataAccess.ExecuteWriteTransactionAsync($"CREATE DATABASE {databaseName}");
}

static IServiceCollection CreateIocContainer(IConfigurationRoot configuration)
{
    var services = new ServiceCollection().UseStartup<Startup>(configuration);
    return services;
}

static IConfigurationBuilder CreateConfigurationBuilder(string[] args)
{
    var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    ArgumentNullException.ThrowIfNullOrEmpty(basePath);

    return new ConfigurationBuilder()
        .SetBasePath(basePath)
        .AddJsonFile("appSettings.json", false, false)
        .AddUserSecrets<Program>();
}
