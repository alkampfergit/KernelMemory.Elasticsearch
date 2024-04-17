using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KernelMemory.ElasticSearch;

internal class ElasticSearchHelper
{
    private readonly KernelMemoryElasticSearchConfig _kernelMemoryElasticSearchConfig;
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticSearchHelper> _logger;

    public ElasticSearchHelper(
        KernelMemoryElasticSearchConfig kernelMemoryElasticSearchConfig,
        ILogger<ElasticSearchHelper>? logger = null)
    {
        _kernelMemoryElasticSearchConfig = kernelMemoryElasticSearchConfig;
        _logger = logger ?? DefaultLogger<ElasticSearchHelper>.Instance;
        var settings = new ElasticsearchClientSettings(new Uri(kernelMemoryElasticSearchConfig.ServerAddress));
#if DEBUG
        settings = settings
            .PrettyJson()
            .DisableDirectStreaming(true);
#else
#endif

        if (!string.IsNullOrEmpty(kernelMemoryElasticSearchConfig.UserName))
        {
            settings.Authentication(new BasicAuthentication(kernelMemoryElasticSearchConfig.UserName, kernelMemoryElasticSearchConfig.Password!));
        }
        _client = new ElasticsearchClient(settings);

        QueryHelper = new ElasticSearchQueryHelper(_client, _logger);
    }

    internal ElasticSearchQueryHelper QueryHelper { get; }

    internal List<string> CreatedIndices { get; } = new();

    internal async Task EnsureIndexAsync(
        string indexName,
        int vectorDimension,
        CancellationToken cancellationToken)
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

                       settings.Analysis(analysis =>
                       {
                           analysis.Analyzers(analyzer =>
                           {
                               analyzer.Custom("nalc", custom => custom
                                    .Tokenizer("keyword")
                                    .Filter(["lowercase"]));
                           });
                       });
                   });

                   cfg.Mappings(mappings =>
                   {
                       mappings.Properties<object>(pm =>
                       {
                           pm.DenseVector("vector", dv => dv.Dims(vectorDimension));
                           pm.Text("payload", pd => pd.Index(false));
                       })
                       .DynamicTemplates(GetDynamicTemplates());
                   });
               },
               cancellationToken).ConfigureAwait(false);

            if (!createIdxResponse.IsValidResponse)
            {
                throw new Exception($"Failed to create index {indexName}");
            }

            CreatedIndices.Add(indexName);
        }
    }

    private ICollection<IDictionary<string, DynamicTemplate>> GetDynamicTemplates()
    {
        var dt = new List<IDictionary<string, DynamicTemplate>>();
        var tags = new Dictionary<string, DynamicTemplate>();
        tags["tags"] = new DynamicTemplate
        {
            Match = "tag_*",
            Mapping = new TextProperty
            {
                Analyzer = "standard",
                Index = true,
                Store = true,
                Fields = new Properties() {
                    { "keyword", new KeywordProperty() },
                    { "na", new TextProperty()
                        {
                            Analyzer = "nalc"
                        }
                    },
                    { "english", new TextProperty()
                        {
                            Analyzer = "english"
                        }
                    }
                }
            },
        };
        dt.Add(tags);

        var txtProp = new Dictionary<string, DynamicTemplate>();
        txtProp["txt"] = new DynamicTemplate
        {
            Match = "txt_*",
            Mapping = new TextProperty
            {
                Analyzer = "standard",
                Index = true,
                Store = true,
                Fields = new Properties() {
                    { "keyword", new KeywordProperty() },
                    { "english", new TextProperty() {
                        Analyzer = "english"
                    } }
                }
            },
        };
        dt.Add(txtProp);
        return dt;
    }

    internal async Task PurgeIndexWithPrefixAsync(string prefix, CancellationToken cancellationToken)
    {
        var gir = new GetIndexRequest(prefix + "*");
        var list = await _client.Indices.GetAsync(gir, cancellationToken);
        foreach (var index in list.Indices)
        {
            await _client.Indices.DeleteAsync(index.Key, cancellationToken);
        }
    }

    internal async Task<TypeMapping?> GetIndexMappingAsync(string indexName)
    {
        await _client.Indices.RefreshAsync(indexName);
        var gmr = new GetMappingRequest(indexName);
        var mapping = await _client.Indices.GetMappingAsync(gmr);
        if (!mapping.IsValidResponse)
        {
            _logger.LogError("Failed retrieve mapping for index {indexName} - {error}",
                indexName,
                mapping.GetErrorFromElasticResponse());
            return null;
        }

        return mapping.Indices[indexName].Mappings;
    }

    internal async Task IndexMemoryRecordAsync(string indexName, MemoryRecord memoryRecord, CancellationToken cancellationToken)
    {
        var indexExists = await _client.Indices.ExistsAsync(indexName, cancellationToken);
        if (!indexExists.Exists)
        {
            throw new KernelMemoryElasticSearchException($"Index {indexName} does not exists");
        }
        var io = ElasticsearchMemoryRecord.ToIndexableObject(
            memoryRecord,
            _kernelMemoryElasticSearchConfig.IndexablePayloadProperties);
        var ir = new IndexRequest<object>(io, indexName, (string)io["id"]);
        var indexResponse = await _client.IndexAsync<object>(ir, cancellationToken);

        if (!indexResponse.IsValidResponse)
        {
            _logger.LogError("Failed Indexing memory record id {id} in index {index} - {error}", memoryRecord.Id, indexName, indexResponse.GetErrorFromElasticResponse());
            throw new KernelMemoryElasticSearchException($"Failed Indexing memory record id {memoryRecord.Id} in index {indexName} - {indexResponse.ElasticsearchServerError?.Error}");
        }
    }

    internal async Task DeleteRecordAsync(string indexName, string id, CancellationToken cancellationToken)
    {
        var indexResponse = await _client.DeleteAsync<object>(indexName, id, cancellationToken);
        if (!indexResponse.IsValidResponse)
        {
            _logger.LogError("Failed deleting memory record id {id} in index {index} - {error}", id, indexName, indexResponse.GetErrorFromElasticResponse());
            throw new KernelMemoryElasticSearchException($"Failed deleting memory record id {id} in index {indexName} - {indexResponse.ElasticsearchServerError?.Error}");
        }
    }

    internal async ValueTask DeleteIndexAsync(string indexName, CancellationToken cancellationToken = default)
    {
        var deleteResult = await _client.Indices.DeleteAsync(indexName, cancellationToken);
        if (!deleteResult.IsValidResponse)
        {
            _logger.LogError("Failed to delete index {indexName} - {error}",
                indexName,
                deleteResult.GetErrorFromElasticResponse());
        }
    }

    internal async Task<JsonElement?> GetAsync(
        string indexName,
        string id,
        CancellationToken none)
    {
        var getResponse = await _client.GetAsync<object>(indexName, id, none);

        if (!getResponse.IsValidResponse)
        {
            _logger.LogError("Failed to get object {id} from index {indexName} - {error}",
                id,
                indexName,
                getResponse.GetErrorFromElasticResponse());
            return null;
        }

        return (JsonElement)getResponse.Source!;
    }

    public ElasticSearchQueryHelper GetQueryHelper()
    {
        return new ElasticSearchQueryHelper(_client, _logger);
    }

    internal async Task<IEnumerable<string>> GetIndexesNamesAsync(CancellationToken cancellationToken)
    {
        var gir = new GetIndexRequest(_kernelMemoryElasticSearchConfig.IndexPrefix + "*");
        var list = await _client.Indices.GetAsync(gir, cancellationToken);
        return list.Indices.Keys.Select(k => k.ToString());
    }

    internal Task RefreshAsync(string indexName)
    {
        return _client.Indices.RefreshAsync(indexName);
    }
}
