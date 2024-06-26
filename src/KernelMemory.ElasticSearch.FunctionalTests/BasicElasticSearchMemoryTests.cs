﻿using KernelMemory.ElasticSearch.FunctionalTests.Doubles;

namespace KernelMemory.ElasticSearch.FunctionalTests
{
    public class BasicElasticSearchMemoryTests : BasicElasticTestFixture
    {
        protected readonly ElasticSearchMemory _sut;

        public BasicElasticSearchMemoryTests(IConfiguration cfg, IServiceProvider serviceProvider) : base(cfg, serviceProvider)
        {
            ChangeConfig(Config);
            _sut = new ElasticSearchMemory(Config!, new TestEmbeddingGenerator());
        }

        protected virtual void ChangeConfig(KernelMemoryElasticSearchConfig? config)
        {
        }

        protected List<string> IndexToDelete = new();

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
        public CreationIndex(IConfiguration cfg, IServiceProvider serviceProvider) : base(cfg, serviceProvider)
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
        public DeletionIndex(IConfiguration cfg, IServiceProvider serviceProvider) : base(cfg, serviceProvider)
        {
        }

        [Fact]
        public async Task CanDeleteIndex()
        {
            IndexToDelete.Add("fff1");
            await _sut.CreateIndexAsync("fff1", 4);

            var realIndexNames = $"{Config!.IndexPrefix}fff1";
            var mapping = await ElasticSearchHelper.GetIndexMappingAsync(realIndexNames);
            Assert.NotNull(mapping);

            await _sut.DeleteIndexAsync("fff1");
            await this.ElasticSearchHelper.RefreshAsync(realIndexNames);

            //ok index must be deleted
            var indexInfo = await ElasticSearchHelper.GetIndexMappingAsync(realIndexNames);
            Assert.Null(indexInfo);
        }
    }

    public class ListIndexes : BasicElasticSearchMemoryTests
    {
        public ListIndexes(IConfiguration cfg, IServiceProvider serviceProvider) : base(cfg, serviceProvider)
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
        public ListIndexesWithoutPrefix(IConfiguration cfg, IServiceProvider serviceProvider) : base(cfg, serviceProvider)
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
