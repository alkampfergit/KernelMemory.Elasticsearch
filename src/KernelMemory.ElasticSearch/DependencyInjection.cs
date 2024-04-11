using KernelMemory.ElasticSearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MongoDbAtlas;

namespace Microsoft.KernelMemory;

public static class KernelMemoryBuilderExtensions
{
    /// <summary>
    /// Adds Mongodb as memory service, to store memory records.
    /// </summary>
    /// <param name="builder">The kernel builder</param>
    /// <param name="config">Configuration for Mongodb</param>
    public static IKernelMemoryBuilder WithElasticSearch(
        this IKernelMemoryBuilder builder,
        KernelMemoryElasticSearchConfig config)
    {
        builder.Services.AddElasticSearchMemory(config);
        return builder;
    }
}

/// <summary>
/// setup ElasticSearchMemory
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds ElasticSearchMemory as a service.
    /// </summary>
    /// <param name="services">The services collection</param>
    /// <param name="config">ElasticSearchMemory configuration.</param>
    public static IServiceCollection AddElasticSearchMemory(
        this IServiceCollection services,
        KernelMemoryElasticSearchConfig config)
    {
        return services
            .AddSingleton(config)
            .AddSingleton<IMemoryDb, ElasticSearchMemory>();
    }
}
