using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using GraphMigrator.Extensions;
using GraphMigrator;
using GraphMigrator.Algorithms.RelationalSchemaExtractors;

var configuration = CreateConfigurationBuilder(args).Build();
var serviceProvider = CreateIocContainer(configuration).BuildServiceProvider();

var extractor = serviceProvider.GetRequiredService<IRelationalSchemaExtractor>();
//var schema = await extractor.GetSchema();

Console.WriteLine("Starting schema analysis...");

var startTime = DateTime.Now;
var schema = await extractor.GetSchema();
var duration = DateTime.Now - startTime;

Console.WriteLine($"\nAnalysis completed in {duration.TotalSeconds:F2} seconds");
Console.WriteLine($"Retrieved information for {schema.Tables.Count} tables");

foreach (var table in schema.Tables)
{
    Console.WriteLine($"\nTable: {table.Name}");

    Console.WriteLine("Columns:");
    foreach (var column in table.Columns)
    {
        string lengthInfo = column.MaxLength.HasValue ? $"({column.MaxLength})" : "";
        Console.WriteLine($"  {column.Name}: {column.DataType}{lengthInfo} {(column.IsNullable ? "NULL" : "NOT NULL")}");
    }

    Console.WriteLine("\nPrimary Keys:");
    foreach (var pk in table.PrimaryKeys)
    {
        Console.WriteLine($"  {pk}");
    }

    Console.WriteLine("\nForeign Keys:");
    foreach (var fk in table.ForeignKeys)
    {
        Console.WriteLine($"  {fk.ColumnName} -> {fk.ReferencedTableName}.{fk.ReferencedColumnName}");
    }

    Console.WriteLine("\n" + new string('-', 50));
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
