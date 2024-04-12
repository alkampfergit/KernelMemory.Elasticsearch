using System;

namespace KernelMemory.ElasticSearch;

/// <summary>
/// Represents configuration to use ElasticSearch as memory storage.
/// </summary>
public class KernelMemoryElasticSearchConfig
{
    public string ServerAddress { get; set; } = null!;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string? IndexPrefix { get; set; } = string.Empty;

    public int ReplicaCount { get; set; } = 1;

    public int ShardNumber { get; set; } = 1;

    /// <summary>
    /// To support full search we can specify some of the
    /// properties in payload to be searchable.
    /// </summary>
    public string[] IndexablePayloadProperties { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Allows to specify connection string
    /// </summary>
    /// <param name="serverAddress">Connection string</param>
    /// <returns></returns>
    public KernelMemoryElasticSearchConfig SetServerAddress(string serverAddress)
    {
        ServerAddress = serverAddress;
        return this;
    }

    public KernelMemoryElasticSearchConfig SetUserCredentials(string userName, string password)
    {
        UserName = userName;
        Password = password;
        return this;
    }

    public KernelMemoryElasticSearchConfig SetIndexPrefix(string indexPrefix)
    {
        IndexPrefix = indexPrefix;
        return this;
    }

    public KernelMemoryElasticSearchConfig SetReplicaCount(int replicaCount)
    {
        ReplicaCount = replicaCount;
        return this;
    }

    public KernelMemoryElasticSearchConfig SetShardNumber(int shardNumber)
    {
        ShardNumber = shardNumber;
        return this;
    }
}
