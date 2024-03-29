using System.Numerics;

namespace KernelMemory.ElasticSearch.FunctionalTests
{
    public class ElasticSearchHelperTests : BasicElasticTestFixture
    {
        public ElasticSearchHelperTests(IConfiguration cfg) : base(cfg)
        {
        }

        [Fact]
        public async Task Can_Create_index_with_mapping()
        {
            Guid guid = Guid.NewGuid();
            BigInteger guidInteger = new BigInteger(guid.ToByteArray());
            string indexName = "kmtest" + guidInteger;

            await ElasticSearchHelper.EnsureIndexAsync(indexName, CancellationToken.None);

            //ok now I want the mapping to the index
            var mapping = await ElasticSearchHelper.GetIndexMappingAsync(indexName);

            Assert.NotNull(mapping);
        }
    }
}
