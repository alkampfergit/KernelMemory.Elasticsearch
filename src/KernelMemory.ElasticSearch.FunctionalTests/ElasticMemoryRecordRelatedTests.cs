using Microsoft.KernelMemory.MemoryStorage;

namespace KernelMemory.ElasticSearch.FunctionalTests;

[Trait("Category", "Record")]
public class ElasticMemoryRecordRelatedTests : BasicElasticTestFixture
{
    public ElasticMemoryRecordRelatedTests(IConfiguration cfg) : base(cfg)
    {
    }

    [Fact]
    public void Serialize_deserialize_from_memoryRecord()
    {
        MemoryRecord mr = GenerateAMemoryRecord();

        var esmr = ElasticsearchMemoryRecord.ToIndexableObject(mr, ["text"]);

        var rmr = ElasticsearchMemoryRecord.MemoryRecordFromIndexableObject(esmr, true);

        CompareMemoryRecords(mr, rmr);
    }

    [Fact]
    public async Task Basic_index_memory_record()
    {
        MemoryRecord mr = GenerateAMemoryRecord();
        string indexName = await CreateIndex(3);

        var indexed = await ElasticSearchHelper.IndexMemoryRecordAsync(indexName, mr, CancellationToken.None);
        Assert.True(indexed);
    }

    [Fact]
    public async Task Basic_retrieve_from_elasticsearch()
    {
        MemoryRecord mr = GenerateAMemoryRecord();
        string indexName = await CreateIndex(3);

        var indexed = await ElasticSearchHelper.IndexMemoryRecordAsync(indexName, mr, CancellationToken.None);

        var jsonElement = await ElasticSearchHelper.GetAsync(indexName, mr.Id, CancellationToken.None);

        Assert.NotNull(jsonElement);

        CompareMemoryRecords(mr, ElasticsearchMemoryRecord.MemoryRecordFromJsonElement(jsonElement.Value, true));

        Assert.True(indexed);
    }
}
