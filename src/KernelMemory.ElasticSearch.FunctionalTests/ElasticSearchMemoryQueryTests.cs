﻿using KernelMemory.ElasticSearch.FunctionalTests.Doubles;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MongoDbAtlas;
using System.Security.Cryptography;

namespace KernelMemory.ElasticSearch.FunctionalTests;

public class ElasticSearchMemoryQueryTests : IClassFixture<ElasticSearchMemoryQueryTestsTestsFixture>
{
    private readonly ElasticSearchMemoryQueryTestsTestsFixture _fixture;
    private readonly TestEmbeddingGenerator _embeddingGenerator;
    private readonly ElasticSearchMemory _sut;

    public ElasticSearchMemoryQueryTests(
        ElasticSearchMemoryQueryTestsTestsFixture fixture)
    {
        this._fixture = fixture;
        _embeddingGenerator = new TestEmbeddingGenerator();
        _sut = new ElasticSearchMemory(_fixture.Config!, _embeddingGenerator);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CanQueryEmptyClause(bool withEmbeddings)
    {
        var results = _sut.GetListAsync(_fixture.IndexName, filters: null, limit: 1, withEmbeddings: withEmbeddings);
        var realResults = await results.ToListAsync();
        Assert.Single(realResults);
        if (withEmbeddings)
        {
            Assert.Equal(4, realResults.Single().Vector.Length);
        }
        else
        {
            Assert.Equal(0, realResults.Single().Vector.Length);
        }
    }

    /// <summary>
    /// WE are not really testing the knn search, we want to simply verify that the query is working.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Smoke_can_Query_with_vector()
    {
        var results = _sut.GetSimilarListAsync(_fixture.IndexName, "sample question", filters: null, limit: 5, withEmbeddings: true);
        var realResults = await results.ToListAsync();
        Assert.Equal(4, realResults.Count);
    }

    [Fact]
    public async Task Brutal_verify_vector()
    {
        const string sampleQuestion = "sample question";
        Embedding qembed = await this._embeddingGenerator.GenerateEmbeddingAsync(sampleQuestion, CancellationToken.None);
        var coll = qembed.Data.ToArray();

        var results = _sut.GetSimilarListAsync(_fixture.IndexName, sampleQuestion, filters: null, limit: 5, withEmbeddings: true);
        var realResults = await results.ToListAsync();
        Assert.Equal(4, realResults.Count);

        //now brute force calculate cosine similarity
        var cosSim = realResults.Select(r => TestUtils.CosineSim(r.Item1.Vector, coll)).ToList();

        //now verify that the cosine similarity is in descending order
        for (int i = 0; i < cosSim.Count - 1; i++)
        {
            Assert.True(cosSim[i] >= cosSim[i + 1]);
        }
    }
}

/// <summary>
/// Creates an index, then save some data to vefify query capabilities.
/// </summary>
public class ElasticSearchMemoryQueryTestsTestsFixture : IAsyncLifetime
{
    public string IndexName { get; private set; } = null!;

    public KernelMemoryElasticSearchConfig Config { get; }

    internal ElasticSearchHelper ElasticSearchHelper { get; }

    public int TotalRecords { get; private set; }

    public ElasticSearchMemoryQueryTestsTestsFixture()
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
        var realIndexName = Config.IndexPrefix + IndexName;
        await ElasticSearchHelper.EnsureIndexAsync(realIndexName, 4, CancellationToken.None);
        //now we can index some data
        await ElasticSearchHelper.IndexMemoryRecordAsync(realIndexName, GenerateAMemoryRecord("red", "nice", [1.0f, 2.0f, 3.0f, 4.0f]), CancellationToken.None);
        await ElasticSearchHelper.IndexMemoryRecordAsync(realIndexName, GenerateAMemoryRecord("blue", "bad", [1.0f, 0.4f, 4.0f, 4.0f]), CancellationToken.None);
        await ElasticSearchHelper.IndexMemoryRecordAsync(realIndexName, GenerateAMemoryRecord("black", "night", [1.0f, 0.8f, 4.0f, 4.0f]), CancellationToken.None);
        await ElasticSearchHelper.IndexMemoryRecordAsync(realIndexName, GenerateAMemoryRecord("black", "day", [1.0f, -0.4f, 4.0f, -4.0f]), CancellationToken.None);
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