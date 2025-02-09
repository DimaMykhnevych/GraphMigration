using GraphMigrator.Algorithms.RelationalSchemaExtractors;
using GraphMigrator.Domain.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GraphMigrator;

public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<IApplicationRunner, ApplicationRunner>();

        services.AddTransient<IRelationalSchemaExtractor, MSSQLExtractor>();

        // Configuration Options
        services.Configure<SourceDataSourceConfiguration>(Configuration.GetSection(nameof(SourceDataSourceConfiguration)));
    }
}

