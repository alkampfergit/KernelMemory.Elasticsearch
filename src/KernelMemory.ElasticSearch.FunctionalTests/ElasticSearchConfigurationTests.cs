namespace KernelMemory.ElasticSearch.FunctionalTests
{
    public class ElasticSearchConfigurationTests
    {
        [Fact]
        public void Can_set_server_address()
        {
            var config = new KernelMemoryElasticSearchConfig();
            config.SetServerAddress("http://localhost:9200");
            Assert.Equal("http://localhost:9200", config.ServerAddress);
        }

        [Fact]
        public void Can_set_user_credentials()
        {
            var config = new KernelMemoryElasticSearchConfig();
            config.SetUserCredentials("user", "password");
            Assert.Equal("user", config.UserName);
            Assert.Equal("password", config.Password);
        }

        [Fact]
        public void Can_set_index_prefix()
        {
            var config = new KernelMemoryElasticSearchConfig();
            config.SetIndexPrefix("prefix");
            Assert.Equal("prefix", config.IndexPrefix);
        }

        [Fact]
        public void Can_set_replica_count()
        {
            var config = new KernelMemoryElasticSearchConfig();
            config.SetReplicaCount(2);
            Assert.Equal(2, config.ReplicaCount);
        }

        [Fact]
        public void Can_set_shard_number()
        {
            var config = new KernelMemoryElasticSearchConfig();
            config.SetShardNumber(3);
            Assert.Equal(3, config.ShardNumber);
        }

        [Fact]
        public void Can_set_indexable_payload_properties()
        {
            var config = new KernelMemoryElasticSearchConfig();
            config.IndexablePayloadProperties = new[] { "prop1", "prop2" };
            Assert.Equal(new[] { "prop1", "prop2" }, config.IndexablePayloadProperties);
        }
    }
}
