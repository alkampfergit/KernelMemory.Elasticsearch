using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KernelMemory.ElasticSearch;

/// <summary>
/// Helps translate the query done with the interfaces of kernel 
/// memory.
/// </summary>
internal class ElasticSearchQueryHelper
{
    private ElasticsearchClient _client;
    private KernelMemoryElasticSearchConfig _kernelMemoryElasticSearchConfig;
    private ILogger<ElasticSearchHelper> _logger;

    internal ElasticSearchQueryHelper(
        ElasticsearchClient client,
        KernelMemoryElasticSearchConfig kernelMemoryElasticSearchConfig,
        ILogger<ElasticSearchHelper> logger)
    {
        _client = client;
        _kernelMemoryElasticSearchConfig = kernelMemoryElasticSearchConfig;
        _logger = logger;
    }

    public async Task<long> VerifyQueryAsync(string index, QueryDescriptor<object> queryDescriptor)
    {
        await _client.Indices.RefreshAsync(index, CancellationToken.None);
        var resp = await _client.SearchAsync<object>(s => s
           .Index(index)
           .Query(queryDescriptor),
            CancellationToken.None);
        return resp.Total;
    }

    public async Task<IReadOnlyCollection<Hit<object>>> ExecuteQueryAsync(
        string index,
        int limit,
        QueryDescriptor<object> queryDescriptor,
        CancellationToken cancellationToken = default)
    {
        await _client.Indices.RefreshAsync(index, cancellationToken);
        var resp = await _client.SearchAsync<object>(s => s
           .Index(index)
           .Query(queryDescriptor)
           .From(0)
           .Size(limit),
            CancellationToken.None);

        //need to return empty if the index does not exists
        if (!resp.IsValidResponse)
        {
            if (resp.ElasticsearchServerError?.Error?.Type == "index_not_found_exception")
            {
                return Array.Empty<Hit<object>>();
            }

            throw new KernelMemoryElasticSearchException($"Error during search: {resp.ElasticsearchServerError?.Error?.Reason}");
        }
        return resp.Hits;
    }
}
