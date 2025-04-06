using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using GraphMigrator.Extensions;
using GraphMigrator;
using GraphMigrator.Algorithms.Neo4jDataLayer;
using GraphMigrator.Algorithms.ImprovedMigrationAlgorithmN;
using GraphMigrator.Algorithms.Rel2Graph;
using GraphMigrator.Algorithms.Rel2GraphParallel;
using GraphMigrator.Algorithms.Performance;
using GraphMigrator.Algorithms.QueryComparison;

var configuration = CreateConfigurationBuilder(args).Build();
var serviceProvider = CreateIocContainer(configuration).BuildServiceProvider();

var dataAccess = serviceProvider.GetRequiredService<INeo4jDataAccess>();
using var improvedAlgo = serviceProvider.GetRequiredService<IImprovedMigrationAlgorithm>();
var rel2graphAlgo = serviceProvider.GetRequiredService<IRel2GraphAlgorithm>();
using var rel2graphParallelAlgo = serviceProvider.GetRequiredService<IRel2GraphParallelAlgorithm>();

// await Experiment1();

//var parallelRel2Graph = await Experiment2(rel2graphParallelAlgo.MigrateToGraphDatabaseAsync);
//Console.WriteLine($"\nREL2GRAPH parallel: Analysis completed in {parallelRel2Graph:F2} seconds");

//var improved = await Experiment2(improvedAlgo.MigrateToGraphDatabaseAsync);
//Console.WriteLine($"\nIMPROVED: Analysis completed in {improved:F2} seconds");

var queryComparison = serviceProvider.GetRequiredService<IQueryComparisonAlgorithm>();
const string sqlQuery = @"SELECT 
 u3.Username AS SuggestedFriend,
 COUNT(DISTINCT u2.UserId) AS CommonFriends
FROM [User] u1
JOIN [Friendship] f1 ON (u1.UserId = f1.InitiatorId OR u1.UserId = 
f1.RequestedFriendId)
JOIN [User] u2 ON (u2.UserId = f1.InitiatorId OR u2.UserId = 
f1.RequestedFriendId) AND u2.UserId <> u1.UserId
JOIN [Friendship] f2 ON (u2.UserId = f2.InitiatorId OR u2.UserId = 
f2.RequestedFriendId)
JOIN [User] u3 ON (u3.UserId = f2.InitiatorId OR u3.UserId = 
f2.RequestedFriendId) AND u3.UserId <> u2.UserId
WHERE 
 u1.Username = 'Cecile52'
 AND u3.UserId <> u1.UserId
 AND f1.Status = 1
 AND f2.Status = 1
 AND NOT EXISTS (
 SELECT 1 FROM [Friendship] f3 
 WHERE f3.Status = 1
 AND ((f3.InitiatorId = u1.UserId AND f3.RequestedFriendId = 
u3.UserId)
 OR (f3.InitiatorId = u3.UserId AND f3.RequestedFriendId = 
u1.UserId))
 )
GROUP BY u3.Username
ORDER BY SuggestedFriend DESC;
";
const string cypherQuery = @"MATCH (u:User {Username: 'Cecile52'})-[:Friendship {Status: 1}]-(friend:User)-[:Friendship {Status: 1}]-(fof:User)
WHERE NOT (u)-[:Friendship {Status: 1}]-(fof) AND u <> fof
RETURN fof.Username AS SuggestedFriend, COUNT(DISTINCT friend) AS CommonFriends
ORDER BY SuggestedFriend DESC;
";
var result = await queryComparison.CompareQueryResults(sqlQuery, cypherQuery);
Console.WriteLine(result.SqlResults.Count);

async Task<double> Experiment2(Func<CancellationToken, Task> algo)
{
    var ct = new CancellationToken();
    List<double> results = [];
    for (int i = 0; i < 10; i++)
    {
        await dataAccess.DeleteSchemaWithData();
        var startTime = DateTime.Now;
        await algo(ct);
        var duration = DateTime.Now - startTime;
        results.Add(duration.TotalSeconds);
    }

    return results.Average();
}

async Task Experiment1()
{
    Console.WriteLine($"\nRunning REL2GRAPH algorithm");
    await Experiment1Wrapper(rel2graphAlgo.MigrateToGraphDatabaseAsync);

    Console.WriteLine($"\nRunning REL2GRAPH parallel algorithm");
    await Experiment1Wrapper(() => rel2graphParallelAlgo.MigrateToGraphDatabaseAsync(new CancellationToken()));

    Console.WriteLine($"\nRunning improved algorithm");
    await Experiment1Wrapper(() => improvedAlgo.MigrateToGraphDatabaseAsync(new CancellationToken()));


    //await dataAccess.DeleteSchemaWithData();

    //var startTime = DateTime.Now;
    //await rel2graphAlgo.MigrateToGraphDatabaseAsync();
    //var duration = DateTime.Now - startTime;
    //Console.WriteLine($"\nREL2GRAPH: Analysis completed in {duration.TotalSeconds:F2} seconds");

    //await dataAccess.DeleteSchemaWithData();

    //startTime = DateTime.Now;
    //await rel2graphParallelAlgo.MigrateToGraphDatabaseAsync(new CancellationToken());
    //duration = DateTime.Now - startTime;
    //Console.WriteLine($"\nREL2GRAPH parallel: Analysis completed in {duration.TotalSeconds:F2} seconds");

    //await dataAccess.DeleteSchemaWithData();

    //startTime = DateTime.Now;
    //await improvedAlgo.MigrateToGraphDatabaseAsync(new CancellationToken());
    //duration = DateTime.Now - startTime;
    //Console.WriteLine($"\nIMPROVED: Analysis completed in {duration.TotalSeconds:F2} seconds");
}

async Task Experiment1Wrapper(Func<Task> algo)
{
    await dataAccess.DeleteSchemaWithData();

    var metrics = await PerformanceMonitor.MeasurePerformanceAsync(() => algo());

    Console.WriteLine($"CPU Usage: {metrics.CpuUsagePercentage}%");
    Console.WriteLine($"Memory Usage: {metrics.MemoryUsageMB} MB");
    Console.WriteLine($"Execution Time: {metrics.ExecutionTime}");
}

//var query = @"Match (p:Post) RETURN count(p) as personCount";
//var query = @"CREATE (p:Post {
//    Post_ID: 3,
//    Content: ""Sample content C# test"",
//    Description: ""Sample description C# test"",
//    CreationDate: datetime()
//}) RETURN true";
//var result = await dataAccess.ExecuteWriteTransactionAsync<bool>(query);
//Console.WriteLine(result);

//var extractor = serviceProvider.GetRequiredService<IRelationalSchemaExtractor>();
////var schema = await extractor.GetSchema();

//Console.WriteLine("Starting schema analysis...");

//var startTime = DateTime.Now;
//var schema = await extractor.GetSchema();
//var duration = DateTime.Now - startTime;

//Console.WriteLine($"\nAnalysis completed in {duration.TotalSeconds:F2} seconds");
//Console.WriteLine($"Retrieved information for {schema.Tables.Count} tables");

//foreach (var table in schema.Tables)
//{
//    Console.WriteLine($"\nTable: {table.Name}");

//    Console.WriteLine("Columns:");
//    foreach (var column in table.Columns)
//    {
//        string lengthInfo = column.MaxLength.HasValue ? $"({column.MaxLength})" : "";
//        Console.WriteLine($"  {column.Name}: {column.DataType}{lengthInfo} {(column.IsNullable ? "NULL" : "NOT NULL")}");
//    }

//    Console.WriteLine("\nPrimary Keys:");
//    foreach (var pk in table.PrimaryKeys)
//    {
//        Console.WriteLine($"  {pk}");
//    }

//    Console.WriteLine("\nForeign Keys:");
//    foreach (var fk in table.ForeignKeys)
//    {
//        Console.WriteLine($"  {fk.ColumnName} -> {fk.ReferencedTableName}.{fk.ReferencedColumnName}");
//    }

//    Console.WriteLine("\n" + new string('-', 50));
//}

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
