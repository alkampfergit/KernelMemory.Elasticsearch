//using KernelMemory.ElasticSearch;
//using Microsoft.KernelMemory;
//using Microsoft.TestHelpers;

//namespace MongoDbAtlas.FunctionalTests;

//public class DefaultTestsConfigurationBase : DefaultTests
//{
//    public DefaultTestsConfigurationBase(IConfiguration cfg, ITestOutputHelper output)
//        : base(cfg, output, multiCollection: false)
//    {
//    }
//}

//internal abstract class DefaultTests : BaseFunctionalTestCase
//{
//    protected internal ElasticSearchHelper Helper { get; }

//    private readonly MemoryServerless _memory;

//    protected DefaultTests(IConfiguration cfg, ITestOutputHelper output, bool multiCollection) : base(cfg, output)
//    {
//        Assert.False(string.IsNullOrEmpty(this.OpenAiConfig.APIKey), "OpenAI API Key is empty");

//        // if you want you can customize.
//        // this.ElasticSearchConfig

//        Helper = new ElasticSearchHelper(this.ElasticSearchConfig);

//        this._memory = new KernelMemoryBuilder()
//            .WithSearchClientConfig(new SearchClientConfig { EmptyAnswer = NotFound })
//            .WithAzureOpenAITextGeneration(this.OpenAiConfig)
//            // .WithAzureOpenAITextGeneration(this.AzureOpenAITextConfiguration)
//            // .WithAzureOpenAITextEmbeddingGeneration(this.AzureOpenAIEmbeddingConfiguration)
//            .WithElasticSearch(this.ElasticSearchConfig)
//            .Build<MemoryServerless>();
//    }

//    [Fact]
//    [Trait("Category", "MongoDbAtlas")]
//    public async Task ItSupportsASingleFilter()
//    {
//        await FilteringTest.ItSupportsASingleFilter(this._memory, this.Log);
//    }

//    [Fact]
//    [Trait("Category", "MongoDbAtlas")]
//    public async Task ItSupportsMultipleFilters()
//    {
//        await FilteringTest.ItSupportsMultipleFilters(this._memory, this.Log);
//    }

//    [Fact]
//    [Trait("Category", "MongoDbAtlas")]
//    public async Task ItIgnoresEmptyFilters()
//    {
//        await FilteringTest.ItIgnoresEmptyFilters(this._memory, this.Log, true);
//    }

//    [Fact]
//    [Trait("Category", "MongoDbAtlas")]
//    public async Task ItListsIndexes()
//    {
//        await IndexListTest.ItListsIndexes(this._memory, this.Log);
//    }

//    [Fact]
//    [Trait("Category", "MongoDbAtlas")]
//    public async Task ItNormalizesIndexNames()
//    {
//        await IndexListTest.ItNormalizesIndexNames(this._memory, this.Log);
//    }

//    [Fact]
//    [Trait("Category", "MongoDbAtlas")]
//    public async Task ItUsesDefaultIndexName()
//    {
//        await IndexListTest.ItUsesDefaultIndexName(this._memory, this.Log, "default4tests");
//    }

//    [Fact]
//    [Trait("Category", "MongoDbAtlas")]
//    public async Task ItDeletesIndexes()
//    {
//        await IndexDeletionTest.ItDeletesIndexes(this._memory, this.Log);
//    }

//    [Fact]
//    [Trait("Category", "MongoDbAtlas")]
//    public async Task ItHandlesMissingIndexesConsistently()
//    {
//        await MissingIndexTest.ItHandlesMissingIndexesConsistently(this._memory, this.Log);
//    }

//    [Fact]
//    [Trait("Category", "MongoDbAtlas")]
//    public async Task ItUploadsPDFDocsAndDeletes()
//    {
//        await DocumentUploadTest.ItUploadsPDFDocsAndDeletes(this._memory, this.Log);
//    }

//    [Fact]
//    [Trait("Category", "MongoDbAtlas")]
//    public async Task ItSupportsTags()
//    {
//        await DocumentUploadTest.ItSupportsTags(this._memory, this.Log);
//    }
//}
