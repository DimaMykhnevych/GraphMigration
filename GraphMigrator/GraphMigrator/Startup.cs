using GraphMigrator.Algorithms.ImprovedMigrationAlgorithmN;
using GraphMigrator.Algorithms.QueryComparison;
using GraphMigrator.Algorithms.Rel2Graph;
using GraphMigrator.Algorithms.Rel2GraphParallel;
using GraphMigrator.Algorithms.RelationalSchemaExtractors;
using GraphMigrator.Domain.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;

namespace GraphMigrator;

public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<IApplicationRunner, ApplicationRunner>();

        services.AddTransient<IRelationalSchemaExtractor, MSSQLExtractor>();

        services.AddTransient<IImprovedMigrationAlgorithm, ImprovedMigrationAlgorithm>();

        services.AddTransient<IRel2GraphAlgorithm, Rel2GraphAlgorithm>();

        services.AddTransient<IRel2GraphParallelAlgorithm, Rel2GraphParallelAlgorithm>();

        // Configuration Options
        services.Configure<SourceDataSourceConfiguration>(Configuration.GetSection(nameof(SourceDataSourceConfiguration)));
        services.Configure<TargetDataSourceConfiguration>(Configuration.GetSection(nameof(TargetDataSourceConfiguration)));
        services.Configure<ImprovedAlgorithmSettings>(Configuration.GetSection(nameof(ImprovedAlgorithmSettings)));
        services.Configure<TargetDatbaseNames>(Configuration.GetSection(nameof(TargetDatbaseNames)));

        // Fetch settings object from configuration
        var settings = new TargetDataSourceConfiguration();
        Configuration.GetSection(nameof(TargetDataSourceConfiguration)).Bind(settings);

        // This is to register your Neo4j Driver Object as a singleton
        services.AddSingleton(GraphDatabase.Driver(settings.Connection, AuthTokens.Basic(settings.User, settings.Password)));
    }
}

