using Microsoft.KernelMemory.MemoryStorage;

namespace KernelMemory.ElasticSearch.FunctionalTests;

[Trait("Category", "Record")]
public class ElasticMemoryRecordRelatedTests : BasicElasticTestFixture
{
    public ElasticMemoryRecordRelatedTests(IConfiguration cfg, IServiceProvider serviceProvider) : base(cfg, serviceProvider)
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

        await ElasticSearchHelper.IndexMemoryRecordAsync(indexName, mr, CancellationToken.None);
    }

    [Fact]
    public async Task Basic_retrieve_from_elasticsearch()
    {
        MemoryRecord mr = GenerateAMemoryRecord();
        string indexName = await CreateIndex(3);

        await ElasticSearchHelper.IndexMemoryRecordAsync(indexName, mr, CancellationToken.None);

        var jsonElement = await ElasticSearchHelper.GetAsync(indexName, mr.Id, CancellationToken.None);

        Assert.NotNull(jsonElement);

        CompareMemoryRecords(mr, ElasticsearchMemoryRecord.MemoryRecordFromJsonElement(jsonElement.Value, true));
    }

    [Fact]
    public void Can_payload_contains_array()
    {
        MemoryRecord mr = GenerateAMemoryRecord();
        mr.Payload.Add("test", new string[] { "alpha", "beta", "gamma" });

        var io = ElasticsearchMemoryRecord.ToIndexableObject(mr, Array.Empty<string>());

        var mrback = ElasticsearchMemoryRecord.MemoryRecordFromIndexableObject(io, true);

        CompareMemoryRecords(mr, mrback);
    }
}
