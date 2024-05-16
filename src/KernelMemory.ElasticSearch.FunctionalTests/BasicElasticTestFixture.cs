using Microsoft.KernelMemory.MemoryStorage;
using System.Numerics;

namespace KernelMemory.ElasticSearch.FunctionalTests;

public class BasicElasticTestFixture : IAsyncDisposable, IDisposable
{
    private string? _indexName;

    public BasicElasticTestFixture(IConfiguration cfg, IServiceProvider serviceProvider)
    {
        var config = cfg.GetSection("KernelMemory:Services:ElasticSearch").Get<KernelMemoryElasticSearchConfig>();
        if (config == null)
        {
            throw new Exception("ElasticSearch config is missing");
        }
        Config = config;
        Config.IndexablePayloadProperties = ["text"];
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        ElasticSearchHelper = new ElasticSearchHelper(Config, loggerFactory.CreateLogger<ElasticSearchHelper>());
    }

    public KernelMemoryElasticSearchConfig Config { get; }

    internal ElasticSearchHelper ElasticSearchHelper { get; }

    protected async Task<string> CreateIndex(int vectorDimension)
    {
        _indexName = GetIndexTestName();

        await ElasticSearchHelper.EnsureIndexAsync(_indexName, vectorDimension, CancellationToken.None);

        return _indexName;
    }

    protected static string GetIndexTestName()
    {
        Guid guid = Guid.NewGuid();
        BigInteger guidInteger = new BigInteger(guid.ToByteArray());
        string indexName = "kmtest" + guidInteger;
        return indexName;
    }

    public ValueTask DisposeAsync()
    {
        foreach (var index in ElasticSearchHelper.CreatedIndices)
        {
            return ElasticSearchHelper.DeleteIndexAsync(index, CancellationToken.None);
        }

        return ValueTask.CompletedTask;
    }

    public virtual void Dispose()
    {
        DisposeAsync().AsTask().Wait();
    }

    protected static void CompareMemoryRecords(MemoryRecord mr, MemoryRecord rmr)
    {
        //Verify everything is equal
        Assert.Equal(mr.Id, rmr.Id);
        Assert.Equal(mr.Vector.Data, rmr.Vector.Data);

        Assert.Equal(mr.Payload, rmr.Payload);
        Assert.Equal(mr.Tags, rmr.Tags);
    }

    protected static MemoryRecord GenerateAMemoryRecord(string documentId = null)
    {
        return new MemoryRecord()
        {
            Id = Guid.NewGuid().ToString(),
            Vector = new float[] { 1.0f, 2.0f, 3.0f },
            Payload = new Dictionary<string, object>()
            {
                { "text", "hello world" },
                {  "blah", "blah ... "}
            },
            Tags = new Microsoft.KernelMemory.TagCollection()
            {
                ["tag1"] = new List<string?>() { "value1" },
                ["tag2"] = new List<string?>() { "value2" },
                ["__document_id"] =  new List<string?>() { documentId },
            }
        };
    }
}
