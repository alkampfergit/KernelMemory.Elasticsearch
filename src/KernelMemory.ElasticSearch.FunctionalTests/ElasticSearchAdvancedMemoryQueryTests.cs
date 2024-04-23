using KernelMemory.ElasticSearch.FunctionalTests.Doubles;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryStorage;
using System.Security.Cryptography;

namespace KernelMemory.ElasticSearch.FunctionalTests;

public class ElasticSearchAdvancedMemoryQueryTests : IClassFixture<ElasticSearchAdvancedMemoryQueryTestsTestsFixture>
{
    private readonly ElasticSearchAdvancedMemoryQueryTestsTestsFixture _fixture;
    private readonly TestEmbeddingGenerator _embeddingGenerator;
    private readonly ElasticSearchMemory _sut;

    public ElasticSearchAdvancedMemoryQueryTests(
        ElasticSearchAdvancedMemoryQueryTestsTestsFixture fixture)
    {
        this._fixture = fixture;
        _embeddingGenerator = new TestEmbeddingGenerator();
        _sut = new ElasticSearchMemory(_fixture.Config!, _embeddingGenerator);
    }

    [Fact]
    public async Task Can_search_a_basic_keyword()
    {
        var result = await _sut.SearchKeywordAsync(_fixture.IndexName, "antiquities", limit: 10, cancellationToken: default).ToListAsync();
        Assert.Single(result);
        var mr = result.Single();
        Assert.Equal(_fixture.MemoryRecords[1].Id, mr.Id);
    }

    [Fact]
    public async Task Can_search_a_basic_keyword_with_empty_filter()
    {
        var result = await _sut.SearchKeywordAsync(_fixture.IndexName, "antiquities", filters: [], limit: 10, cancellationToken: default).ToListAsync();
        Assert.Single(result);
        var mr = result.Single();
        Assert.Equal(_fixture.MemoryRecords[1].Id, mr.Id);

        result = await _sut.SearchKeywordAsync(_fixture.IndexName, "antiquities", filters: [new MemoryFilter()], limit: 10, cancellationToken: default).ToListAsync();
        Assert.Single(result);
        mr = result.Single();
        Assert.Equal(_fixture.MemoryRecords[1].Id, mr.Id);
    }

    [Fact]
    public async Task Can_search_a_basic_keyword_with_memory_filter()
    {
        var mf = new MemoryFilter();
        mf.ByTag("tag2", "day");
        var result = await _sut.SearchKeywordAsync(_fixture.IndexName, "earth", filters: [mf], limit: 10, cancellationToken: default).ToListAsync();
        Assert.Single(result);
        var mr = result.Single();
        Assert.Equal(_fixture.MemoryRecords[3].Id, mr.Id);
    }

    [Fact]
    public async Task Can_search_a_basic_keyword_with_memory_filter_or()
    {
        var mf1 = new MemoryFilter();
        mf1.ByTag("tag2", "day");
        var mf2 = new MemoryFilter();
        mf2.ByTag("tag2", "bad");
        var result = await _sut.SearchKeywordAsync(_fixture.IndexName, "earth", filters: [mf1, mf2], limit: 10, cancellationToken: default).ToListAsync();
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Id == _fixture.MemoryRecords[1].Id);
        Assert.Contains(result, r => r.Id == _fixture.MemoryRecords[3].Id);
    }
}

/// <summary>
/// Creates an index, then save some data to vefify query capabilities.
/// </summary>
public class ElasticSearchAdvancedMemoryQueryTestsTestsFixture : IAsyncLifetime
{
    public string IndexName { get; private set; } = null!;

    public KernelMemoryElasticSearchConfig Config { get; }

    internal ElasticSearchHelper ElasticSearchHelper { get; }

    public int TotalRecords { get; private set; }

    public ElasticSearchAdvancedMemoryQueryTestsTestsFixture()
    {
        Config = Startup.Configuration
            .GetSection("KernelMemory:Services:ElasticSearch")
            .Get<KernelMemoryElasticSearchConfig>()!;
        //Important choose which property to index.
        Config!.IndexablePayloadProperties = ["text"];
        ElasticSearchHelper = new ElasticSearchHelper(Config);
    }

    public List<MemoryRecord> MemoryRecords = new();

    public async Task InitializeAsync()
    {
        IndexName = GetIndexTestName();
        var realIndexName = Config.IndexPrefix + IndexName;
        await ElasticSearchHelper.EnsureIndexAsync(realIndexName, 4, CancellationToken.None);
        //now we can index some data
        await ElasticSearchHelper.IndexMemoryRecordAsync(
            realIndexName,
            GenerateAMemoryRecord("red", "nice", [1.0f, 2.0f, 3.0f, 4.0f], "this is awesome content"), CancellationToken.None);
        await ElasticSearchHelper.IndexMemoryRecordAsync(
            realIndexName,
            GenerateAMemoryRecord("blue", "bad", [1.0f, 0.4f, 4.0f, 4.0f], "I'm a real collector of earth antiquities"), CancellationToken.None);
        await ElasticSearchHelper.IndexMemoryRecordAsync(
            realIndexName,
            GenerateAMemoryRecord("black", "night", [1.0f, 0.8f, 4.0f, 4.0f], "Some simple text with earth for indexing"), CancellationToken.None);
        await ElasticSearchHelper.IndexMemoryRecordAsync(
            realIndexName,
            GenerateAMemoryRecord("black", "day", [1.0f, -0.4f, 4.0f, -4.0f],"the moon is the only earth satellite"), CancellationToken.None);
        await ElasticSearchHelper.IndexMemoryRecordAsync(
          realIndexName,
          GenerateAMemoryRecord("blue", "day", [1.0f, -0.4f, 4.0f, -4.0f], "This is a really aliend content"), CancellationToken.None);
    }

    internal MemoryRecord GenerateAMemoryRecord(
        string tag1,
        string tag2,
        float[] vector,
        string text)
    {
        TotalRecords++;
        var mr = new MemoryRecord()
        {
            Id = Guid.NewGuid().ToString(),
            Vector = vector,
            Payload = new Dictionary<string, object>()
                {
                    { "text", text },
                    {  "blah", "blah ... "},
                    { "num", 1 },
                    { "bool", true },
                    { "bool2", false }
                },
            Tags = new Microsoft.KernelMemory.TagCollection()
            {
                ["tag1"] = new List<string?>() { tag1 },
                ["tag2"] = new List<string?>() { tag2 },
            }
        };

        MemoryRecords.Add(mr);

        return mr;
    }

    private string GetIndexTestName()
    {
        var rng = RandomNumberGenerator.GetInt32(0, 1000);
        string indexName = "kmtest" + rng;
        return indexName;
    }

    public Task DisposeAsync()
    {
        //we can drop the index when finished
        return ElasticSearchHelper.PurgeIndexWithPrefixAsync(IndexName, CancellationToken.None);
    }
}