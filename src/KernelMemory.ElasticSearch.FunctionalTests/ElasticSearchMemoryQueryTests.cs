using KernelMemory.ElasticSearch.FunctionalTests.Doubles;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryStorage;
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

    [Fact]
    public void Embedding_generator_is_required()
    {
        Assert.Throws<ArgumentNullException>(() => new ElasticSearchMemory(_fixture.Config!, null!));
    }

    [Fact]
    public async Task Cannot_index_in_null_index()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.UpsertAsync("", _fixture.GenerateAMemoryRecord("a", "B", [1, 2, 3,4f])));
    }

    [Fact]
    public async Task When_delete_record_from_not_existing_throws()
    {
        var mr = _fixture.GenerateAMemoryRecord("tagaaaaa", "Tag_bbbbb", [1.0f, 2.0f, 3.0f, 4.0f]);
        await Assert.ThrowsAsync<KernelMemoryElasticSearchException>(async () => await _sut.DeleteAsync("not_exist_index", mr));
    }

    [Fact]
    public async Task Can_delete_not_existing_record()
    {
        var indexName = "testkm" + RandomNumberGenerator.GetInt32(0, 60000);
        await _sut.CreateIndexAsync(indexName, 4);
        await _sut.DeleteAsync(indexName, _fixture.GenerateAMemoryRecord("xyz", "yyy", [1.0f, 2.0f, 3.0f, 4.0f]));
    }

    [Fact]
    public async Task When_add_record_to_not_existing_index_return_false()
    {
        var mr = _fixture.GenerateAMemoryRecord("tagaaaaa", "Tag_bbbbb", [1.0f, 2.0f, 3.0f, 4.0f]);
        await Assert.ThrowsAsync<KernelMemoryElasticSearchException>(async () => await _sut.UpsertAsync("not_exist_index", mr));
    }

    [Fact]
    public async Task Can_insert_then_delete_memory_record()
    {
        var indexName = "testkm" + RandomNumberGenerator.GetInt32(0, 60000);
        var realIndexName = _fixture.Config.IndexPrefix + indexName;
        try
        {
            await _sut.CreateIndexAsync(indexName, 4);
            var mr = _fixture.GenerateAMemoryRecord("tagaaaaa", "Tag_bbbbb", [1.0f, 2.0f, 3.0f, 4.0f]);
            var id = await _sut.UpsertAsync(indexName, mr);
            Assert.Equal(mr.Id, id);

            //search index and verify the record
            await _fixture.ElasticSearchHelper.RefreshAsync(realIndexName);

            var results = _sut.GetListAsync(indexName, filters: null, limit: 1, withEmbeddings: true);
            var realResults = await results.ToListAsync();
            Assert.Single(realResults);
            Assert.Equal(mr.Id, realResults.Single().Id);

            //now delete the record and verify index is empty
            await _sut.DeleteAsync(indexName, mr);
            await _fixture.ElasticSearchHelper.RefreshAsync(realIndexName);

            results = _sut.GetListAsync(indexName, filters: null, limit: 1, withEmbeddings: true);
            realResults = await results.ToListAsync();
            Assert.Empty(realResults);
        }
        finally
        {
            await _fixture.ElasticSearchHelper.DeleteIndexAsync(realIndexName);
        }
    }

    [Fact]
    public async Task Search_in_non_existing_index_should_return_null()
    {
        var results = _sut.GetListAsync("index_does_not_exists_", filters: null, limit: 1, withEmbeddings: true);
        var realResults = await results.ToListAsync();
        Assert.Empty(realResults);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task CanQueryEmptyClause(bool withEmbeddings, bool nullFilter)
    {
        //filter passing both null and empty 
        MemoryFilter[]? filter = nullFilter ? null : Array.Empty<MemoryFilter>();
        var results = _sut.GetListAsync(_fixture.IndexName, filters: filter, limit: 1, withEmbeddings: withEmbeddings);
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

    [Fact]
    public async Task Query_with_filter_with_empty_value()
    {
        //filter passing both null and empty 
        MemoryFilter filter = new();
        filter.ByTag("tag1", ""); //we are filtering with empty value this means that we do not want to filter the tag.
        var results = _sut.GetListAsync(_fixture.IndexName, filters: [filter], limit: 10, withEmbeddings: false);
        var realResults = await results.ToListAsync();
        Assert.Equal(4, realResults.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Tolerate_invalid_limit(int limit)
    {
        //filter passing both null and empty 
        var results = _sut.GetListAsync(_fixture.IndexName, filters: [], limit: limit, withEmbeddings: false);
        var realResults = await results.ToListAsync();
        Assert.Equal(4, realResults.Count);
    }

    [Fact]
    public async Task CanQuery_with_multiple_clause()
    {
        MemoryFilter filter1 = new();
        filter1.ByTag("tag1", "Red");

        MemoryFilter filter2 = new();
        filter2.ByTag("tag2", "night");

        //this is an or composition
        var results = _sut.GetListAsync(_fixture.IndexName, filters: [filter1, filter2], limit: 4, withEmbeddings: false);
        var realResults = await results.ToListAsync();
        Assert.Equal(2, realResults.Count);
    }

    [Fact]
    public async Task CanQuery_with_multiple_clause_composed()
    {
        MemoryFilter filter1 = new();
        filter1.ByTag("tag1", "Red");

        MemoryFilter filter2 = new();
        filter2.ByTag("tag1", "black");
        filter2.ByTag("tag2", "night");

        //this is an or composition
        var results = _sut.GetListAsync(_fixture.IndexName, filters: [filter1, filter2], limit: 4, withEmbeddings: false);
        var realResults = await results.ToListAsync();
        Assert.Equal(2, realResults.Count);
    }

    [Fact]
    public async Task CanQuery_with_multiple_clause_or_empty()
    {
        MemoryFilter filter1 = new();
        filter1.ByTag("tag1", "Red");

        MemoryFilter filter2 = new();

        //this is an or composition
        var results = _sut.GetListAsync(_fixture.IndexName, filters: [filter1, filter2], limit: 4, withEmbeddings: false);
        var realResults = await results.ToListAsync();
        Assert.Single(realResults); //second filter is null so it is filtered out.
    }

    [Fact]
    public async Task CanQuery_with_or()
    {
        MemoryFilter filter1 = new();
        filter1.ByTag("tag1", "black");
        filter1.ByTag("tag2", "night");
        var results = _sut.GetListAsync(_fixture.IndexName, filters: [filter1], limit: 4, withEmbeddings: false);
        var realResults = await results.ToListAsync();
        Assert.Single(realResults);

        var singleResult = realResults.Single();
        Assert.Equal("black", singleResult.Tags["tag1"].Single());
        Assert.Equal("night", singleResult.Tags["tag2"].Single());
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

    internal MemoryRecord GenerateAMemoryRecord(
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