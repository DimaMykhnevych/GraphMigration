using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using GraphMigrator.Extensions;
using GraphMigrator;

var configuration = CreateConfigurationBuilder(args).Build();
var serviceProvider = CreateIocContainer(configuration).BuildServiceProvider();

var runner = serviceProvider.GetRequiredService<IApplicationRunner>();
runner.Run();

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
        .AddJsonFile("appSettings.json", false, false);
}
