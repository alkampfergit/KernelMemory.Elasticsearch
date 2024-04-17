using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryStorage;
using System.Numerics;

namespace KernelMemory.ElasticSearch.FunctionalTests;

public class ElasticSearchQueryHelperTests : IClassFixture<ElasticSearchQueryHelperTestsFixture>
{
    private readonly ElasticSearchQueryHelperTestsFixture _fixture;

    public ElasticSearchQueryHelperTests(ElasticSearchQueryHelperTestsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async void Empty_query_return_all()
    {
        //need to apply the condition for query
        var queryHelper = _fixture.ElasticSearchHelper.GetQueryHelper();
        var translatedQuery = ElasticSearchMemoryFilterConverter.CreateQueryDescriptorFromMemoryFilter([new MemoryFilter()]);
        var results = await queryHelper.VerifyQueryAsync(_fixture.IndexName, translatedQuery);
        Assert.Equal(_fixture.TotalRecords, results);
    }

    [Fact]
    public async void No_query_returns_all()
    {
        //need to apply the condition for query
        var queryHelper = _fixture.ElasticSearchHelper.GetQueryHelper();
        var translatedQuery = ElasticSearchMemoryFilterConverter.CreateQueryDescriptorFromMemoryFilter([]);
        var results = await queryHelper.VerifyQueryAsync(_fixture.IndexName, translatedQuery);
        Assert.Equal(_fixture.TotalRecords, results);
    }

    [Fact]
    public async void Verify_basic_query_with_filter()
    {
        var mf = new MemoryFilter();
        mf.ByTag("tag1", "red");

        //need to apply the condition for query
        var queryHelper = _fixture.ElasticSearchHelper.GetQueryHelper();
        var translatedQuery = ElasticSearchMemoryFilterConverter.CreateQueryDescriptorFromMemoryFilter([mf]);
        var results = await queryHelper.VerifyQueryAsync(_fixture.IndexName, translatedQuery);
        Assert.Equal(1, results);
    }

    [Theory]
    [InlineData("RED")]
    [InlineData("Red")]
    [InlineData("rEd")]
    public async void Verify_basic_query_with_filter_is_case_insensitive(string color)
    {
        var mf = new MemoryFilter();
        mf.ByTag("tag1", color);

        //need to apply the condition for query
        var queryHelper = _fixture.ElasticSearchHelper.GetQueryHelper();
        var translatedQuery = ElasticSearchMemoryFilterConverter.CreateQueryDescriptorFromMemoryFilter([mf]);
        var results = await queryHelper.VerifyQueryAsync(_fixture.IndexName, translatedQuery);
        Assert.Equal(1, results);
    }

    [Fact]
    public async void Verify_double_conditions()
    {
        //this is an and condition, so we need to have both conditions to be true.
        var mf = new MemoryFilter();
        mf.ByTag("tag1", "black");
        mf.ByTag("tag2", "day");

        //need to apply the condition for query
        var queryHelper = _fixture.ElasticSearchHelper.GetQueryHelper();
        var translatedQuery = ElasticSearchMemoryFilterConverter.CreateQueryDescriptorFromMemoryFilter([mf]);
        var results = await queryHelper.VerifyQueryAsync(_fixture.IndexName, translatedQuery);
        Assert.Equal(1, results);
    }

    [Fact]
    public async void Verify_or_conditions()
    {
        //this is an and condition, so we need to have both conditions to be true.
        var mf = new MemoryFilter();
        mf.ByTag("tag1", "black");
        var mf2 = new MemoryFilter();
        mf2.ByTag("tag2", "day");

        //need to apply the condition for query, these two are ORed togheter
        var queryHelper = _fixture.ElasticSearchHelper.GetQueryHelper();
        var translatedQuery = ElasticSearchMemoryFilterConverter.CreateQueryDescriptorFromMemoryFilter([mf, mf2]);
        var results = await queryHelper.VerifyQueryAsync(_fixture.IndexName, translatedQuery);
        Assert.Equal(2, results);
    }
}

/// <summary>
/// Creates an index, then save some data to vefify query capabilities.
/// </summary>
public class ElasticSearchQueryHelperTestsFixture : IAsyncLifetime
{
    public string IndexName { get; private set; } = string.Empty;

    public KernelMemoryElasticSearchConfig Config { get; }

    internal ElasticSearchHelper ElasticSearchHelper { get; }

    public int TotalRecords { get; private set; }

    public ElasticSearchQueryHelperTestsFixture()
    {
        Config = Startup.Configuration
            .GetSection("KernelMemory:Services:ElasticSearch")
            .Get<KernelMemoryElasticSearchConfig>()!;
        Config!.IndexablePayloadProperties = ["text"];
        ElasticSearchHelper = new ElasticSearchHelper(Config);
    }

    public async Task InitializeAsync()
    {
        IndexName = GetIndexTestName();
        await ElasticSearchHelper.EnsureIndexAsync(IndexName, 3, CancellationToken.None);
        //now we can index some data
        await ElasticSearchHelper.IndexMemoryRecordAsync(IndexName, GenerateAMemoryRecord("red", "nice", [1.0f, 2.0f, 3.0f]), CancellationToken.None);
        await ElasticSearchHelper.IndexMemoryRecordAsync(IndexName, GenerateAMemoryRecord("blue", "bad", [1.0f, 0.4f, 4.0f]), CancellationToken.None);
        await ElasticSearchHelper.IndexMemoryRecordAsync(IndexName, GenerateAMemoryRecord("black", "night", [1.0f, 0.4f, 4.0f]), CancellationToken.None);
        await ElasticSearchHelper.IndexMemoryRecordAsync(IndexName, GenerateAMemoryRecord("black", "day", [1.0f, 0.4f, 4.0f]), CancellationToken.None);
    }

    protected MemoryRecord GenerateAMemoryRecord(
        string tag1,
        string tag2,
        float[] vector)
    {
        TotalRecords++;
        return new MemoryRecord()
        {
            Id = Guid.NewGuid().ToString(),
            Vector = vector,
            Payload = new Dictionary<string, object>()
                {
                    { "text", "hello world" },
                    {  "blah", "blah ... "}
                },
            Tags = new Microsoft.KernelMemory.TagCollection()
            {
                ["tag1"] = new List<string?>() { tag1 },
                ["tag2"] = new List<string?>() { tag2 },
            }
        };
    }

    private static string GetIndexTestName()
    {
        Guid guid = Guid.NewGuid();
        BigInteger guidInteger = new BigInteger(guid.ToByteArray());
        string indexName = "kmtest" + guidInteger;
        return indexName;
    }

    public Task DisposeAsync()
    {
        //we can drop the index when finished
        return ElasticSearchHelper.PurgeIndexWithPrefixAsync(IndexName, CancellationToken.None);
    }
}
