namespace KernelMemory.ElasticSearch.FunctionalTests;

public class Startup
{
    public void ConfigureHost(IHostBuilder hostBuilder)
    {
        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json", optional: true)
            .AddUserSecrets<Startup>()
            .AddEnvironmentVariables()
            .Build();

        hostBuilder.ConfigureHostConfiguration(builder => builder.AddConfiguration(Configuration));

        var helper = new ElasticSearchHelper(Configuration.GetSection("KernelMemory:Services:ElasticSearch").Get<KernelMemoryElasticSearchConfig>()!);

        helper.PurgeIndexWithPrefixAsync("testkm", CancellationToken.None).Wait();
        helper.PurgeIndexWithPrefixAsync("kmtest", CancellationToken.None).Wait();
    }

    internal static IConfiguration Configuration { get; private set; } = null!;
}
