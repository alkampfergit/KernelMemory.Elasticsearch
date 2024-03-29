namespace KernelMemory.ElasticSearch.FunctionalTests
{
    public class BasicElasticTestFixture
    {
        public BasicElasticTestFixture(IConfiguration cfg)
        {
            Config = cfg.GetSection("KernelMemory:Services:ElasticSearch").Get<KernelMemoryElasticSearchConfig>();
            ElasticSearchHelper = new ElasticSearchHelper(Config);
        }

        public KernelMemoryElasticSearchConfig? Config { get; }
        internal ElasticSearchHelper ElasticSearchHelper { get; }
    }
}
