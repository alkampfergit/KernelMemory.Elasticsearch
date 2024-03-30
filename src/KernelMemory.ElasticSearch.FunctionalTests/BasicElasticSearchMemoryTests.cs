using KernelMemory.ElasticSearch.FunctionalTests.Doubles;
using Microsoft.KernelMemory.MongoDbAtlas;

namespace KernelMemory.ElasticSearch.FunctionalTests
{
    public class BasicElasticSearchMemoryTests : BasicElasticTestFixture
    {
        protected readonly ElasticSearchMemory _sut;

        public BasicElasticSearchMemoryTests(IConfiguration cfg) : base(cfg)
        {
            _sut = new ElasticSearchMemory(Config!, new TestEmbeddingGenerator());
        }

        protected List<string> IndexToDelete = new List<string>();

        public override void Dispose()
        {
            base.Dispose();

            foreach (var index in IndexToDelete)
            {
                ElasticSearchHelper.DeleteIndexAsync($"{Config!.IndexPrefix}{index}").AsTask().Wait();
            }
        }
    }

    public class CreationIndex : BasicElasticSearchMemoryTests
    {
        public CreationIndex(IConfiguration cfg) : base(cfg)
        {
        }

        [Fact]
        public async Task CanCreateIndex()
        {
            IndexToDelete.Add("test1");
            await _sut.CreateIndexAsync("test1", 4);

            //we need to check if the index was created with the correct prefix.
            var indexInfo = await ElasticSearchHelper.GetIndexMappingAsync("testkmtest1");
            Assert.NotNull(indexInfo);
        }
    }

    public class DeletionIndex : BasicElasticSearchMemoryTests
    {
        public DeletionIndex(IConfiguration cfg) : base(cfg)
        {
        }

        [Fact]
        public async Task CanDeleteIndex()
        {
            IndexToDelete.Add("test1");
            await _sut.CreateIndexAsync("test1", 4);
            await _sut.DeleteIndexAsync("test1");

            //we need to check if the index was created with the correct prefix.
            var indexInfo = await ElasticSearchHelper.GetIndexMappingAsync("testkmtest1");
            Assert.Null(indexInfo);
        }
    }
}
