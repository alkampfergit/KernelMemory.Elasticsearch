using KernelMemory.ElasticSearch.FunctionalTests.Doubles;
using Microsoft.KernelMemory.MongoDbAtlas;

namespace KernelMemory.ElasticSearch.FunctionalTests
{
    public class BasicElasticSearchMemoryTests : BasicElasticTestFixture
    {
        protected readonly ElasticSearchMemory _sut;

        public BasicElasticSearchMemoryTests(IConfiguration cfg) : base(cfg)
        {
            ChangeConfig(Config);
            _sut = new ElasticSearchMemory(Config!, new TestEmbeddingGenerator());
        }

        protected virtual void ChangeConfig(KernelMemoryElasticSearchConfig? config)
        {
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
            IndexToDelete.Add("fff1");
            await _sut.CreateIndexAsync("fff1", 4);
            await _sut.DeleteIndexAsync("fff1");

            //we need to check if the index was created with the correct prefix.
            var indexInfo = await ElasticSearchHelper.GetIndexMappingAsync("testkmtest1");
            Assert.Null(indexInfo);
        }
    }

    public class ListIndexes : BasicElasticSearchMemoryTests
    {
        public ListIndexes(IConfiguration cfg) : base(cfg)
        {
        }

        [Fact]
        public async Task CanListIndex()
        {
            string[] indexes = ["xxx1", "xxx2", "xxx3"];
            foreach (string index in indexes)
            {
                IndexToDelete.Add(index);
                await _sut.CreateIndexAsync(index, 4);
            }

            var realIndexNames = indexes.Select(i => $"{Config!.IndexPrefix}{i}");

            //we need to check if the index was created with the correct prefix.
            var indexInfo = await ElasticSearchHelper.GetIndexesNamesAsync(CancellationToken.None);

            //Verify all expected indices are presemt, we cah nave extra index because you know, other test runs 
            //in parallel
            Assert.Equal(3, indexInfo.Intersect(realIndexNames).Count());
        }
    }

    public class ListIndexesWithoutPrefix : BasicElasticSearchMemoryTests
    {
        public ListIndexesWithoutPrefix(IConfiguration cfg) : base(cfg)
        {
        }

        protected override void ChangeConfig(KernelMemoryElasticSearchConfig? config)
        {
            config!.IndexPrefix = "";
        }

        [Fact]
        public async Task CanListIndex()
        {
            string[] indexes = ["yyy1", "yyy2", "yyy3"];
            foreach (string index in indexes)
            {
                IndexToDelete.Add(index);
                await _sut.CreateIndexAsync(index, 4);
            }

            var realIndexNames = indexes.Select(i => $"{Config.IndexPrefix}{i}");

            //we need to check if the index was created with the correct prefix.
            var indexInfo = await ElasticSearchHelper.GetIndexesNamesAsync(CancellationToken.None);

            //Verify that the list of indices contains the expected indices
            Assert.Equal(3, indexes.Intersect(realIndexNames).Count());
        }
    }
}
