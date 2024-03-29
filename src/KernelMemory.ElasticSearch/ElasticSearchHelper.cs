using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Transport;
using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace KernelMemory.ElasticSearch
{
    internal class ElasticSearchHelper
    {
        private readonly KernelMemoryElasticSearchConfig _kernelMemoryElasticSearchConfig;
        private ElasticsearchClient _client;

        public ElasticSearchHelper(KernelMemoryElasticSearchConfig kernelMemoryElasticSearchConfig)
        {
            _kernelMemoryElasticSearchConfig = kernelMemoryElasticSearchConfig;
            var settings = new ElasticsearchClientSettings(new Uri(kernelMemoryElasticSearchConfig.ServerAddress));
#if DEBUG
            settings = settings
                //.PrettyJson()
                .DisableDirectStreaming(true);
#else
#endif

            if (!string.IsNullOrEmpty(kernelMemoryElasticSearchConfig.UserName)) 
            {
                settings.Authentication(new BasicAuthentication(kernelMemoryElasticSearchConfig.UserName, kernelMemoryElasticSearchConfig.Password));
            }
            _client = new ElasticsearchClient(settings);
        }

        internal async Task EnsureIndexAsync(string indexName, CancellationToken cancellationToken)
        {
            // step1: verify if the index exists
            var exists = await _client.Indices.ExistsAsync(indexName, cancellationToken);
            if (!exists.Exists)
            {
                // index does not exists we neeed to create.
                var createIdxResponse = await _client.Indices.CreateAsync(indexName,
                   cfg =>
                   {
                       cfg.Settings(settings =>
                       {
                           settings.NumberOfShards(_kernelMemoryElasticSearchConfig.ShardNumber);
                           settings.NumberOfReplicas(_kernelMemoryElasticSearchConfig.ReplicaCount);
                       });

                       Properties properties = new();
                       cfg.Mappings(mappings =>
                       {
                           mappings.Properties(properties);
                       });
                   },
                   cancellationToken).ConfigureAwait(false);

                if (!createIdxResponse.IsSuccess())
                {
                    throw new Exception($"Failed to create index {indexName}");
                }
            }
        }

        internal async Task PurgeIndexWithPrefix(string prefix, CancellationToken cancellationToken)
        {
            var gir = new GetIndexRequest(prefix + "*");
            var list = await _client.Indices.GetAsync(gir, cancellationToken);
            foreach (var index in list.Indices)
            {
                await _client.Indices.DeleteAsync(index.Key, cancellationToken);
            }
        }

        internal async Task<JsonObject> GetIndexMappingAsync(string indexName)
        {
            throw new NotImplementedException();
        }
    }
}
