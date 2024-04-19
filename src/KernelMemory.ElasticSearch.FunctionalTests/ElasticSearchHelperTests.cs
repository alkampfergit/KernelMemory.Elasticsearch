using Elastic.Clients.Elasticsearch.Mapping;
using Microsoft.KernelMemory.MemoryStorage;

namespace KernelMemory.ElasticSearch.FunctionalTests;

public class ElasticSearchHelperTests : BasicElasticTestFixture
{
    public ElasticSearchHelperTests(IConfiguration cfg, IServiceProvider serviceProvider) : base(cfg, serviceProvider)
    {

    }

    [Fact]
    public async Task Can_Create_index_with_mapping()
    {
        string indexName = GetIndexTestName();
        await ElasticSearchHelper.EnsureIndexAsync(indexName, 1538, CancellationToken.None);

        //ok now I want the mapping to the index
        var mapping = await ElasticSearchHelper.GetIndexMappingAsync(indexName);

        Assert.NotNull(mapping);
        Assert.NotNull(mapping.DynamicTemplates);

        Assert.True(mapping.DynamicTemplates.Count == 2);
        var tagTemplate = mapping.DynamicTemplates.Single(d => d.Keys.Contains("tags"));
        var tagTemplateKey = tagTemplate["tags"];
        Assert.NotNull(tagTemplateKey);
        Assert.Equal("tag_*", tagTemplateKey.Match);

        var tagmapping = (TextProperty?)tagTemplateKey.Mapping;
        Assert.NotNull(tagmapping);
        Assert.Equal("standard", tagmapping.Analyzer);
        Assert.Equal(3, tagmapping.Fields!.Count());

        var nalcField = (TextProperty?) tagmapping.Fields!["na"];
        Assert.NotNull(nalcField);
        Assert.Equal("nalc", nalcField.Analyzer);

        //verify mapping of the payload properties prefixed with txt
        var txtTemplate = mapping.DynamicTemplates.Single(d => d.Keys.Contains("txt"));
        var txtTemplateKey = txtTemplate["txt"];
        Assert.NotNull(txtTemplateKey);
        Assert.Equal("txt_*", txtTemplateKey.Match);
    }

    [Fact]
    public async Task Create_payload_required_properties()
    {
        MemoryRecord mr = GenerateAMemoryRecord();
        string indexName = await CreateIndex(3);

        await ElasticSearchHelper.IndexMemoryRecordAsync(indexName, mr, CancellationToken.None);

        //now we need to verify the mapping
        var mapping = await ElasticSearchHelper.GetIndexMappingAsync(indexName);

        Assert.NotNull(mapping);
        Assert.NotNull(mapping.Properties);
        //assert we have txt_text property
        Assert.Contains(mapping.Properties, p => p.Key == "txt_text");
        Assert.DoesNotContain(mapping.Properties, p => p.Key == "txt_blah");
        var textProperty = mapping.Properties["txt_text"];
        Assert.Equal("text", textProperty.Type);
    }

    //[Fact]
    //public async Task DeleteAllTestIndices()
    //{
    //    await ElasticSearchHelper.PurgeIndexWithPrefixAsync("testkm", CancellationToken.None);
    //}
}
