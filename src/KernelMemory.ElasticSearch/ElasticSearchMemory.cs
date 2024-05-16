using Elastic.Clients.Elasticsearch.QueryDsl;
using KernelMemory.Extensions.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KernelMemory.ElasticSearch;

/// <summary>
/// Implementation of <see cref="IMemoryDb"/> based on MongoDB Atlas.
/// </summary>
public class ElasticSearchMemory : IMemoryDb, IKernelMemoryExtensionMemoryDb, IBulkMemoryDb
{
    private readonly ITextEmbeddingGenerator _embeddingGenerator;
    private readonly ILogger<ElasticSearchMemory> _log;
    private readonly ElasticSearchHelper _utils;

    private readonly KernelMemoryElasticSearchConfig _config;

    /// <summary>
    /// Create a new instance of MongoDbVectorMemory from configuration
    /// </summary>
    /// <param name="config">Configuration</param>
    /// <param name="embeddingGenerator">Embedding generator</param>
    /// <param name="log">Application logger</param>
    public ElasticSearchMemory(
        KernelMemoryElasticSearchConfig config,
        ITextEmbeddingGenerator embeddingGenerator,
        ILogger<ElasticSearchMemory>? log = null)
    {
        _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
        _log = log ?? DefaultLogger<ElasticSearchMemory>.Instance;
        _config = config;
        _utils = new ElasticSearchHelper(_config);
    }

    /// <inheritdoc />
    public Task CreateIndexAsync(string index, int vectorSize, CancellationToken cancellationToken = default)
    {
        string normalized = GetRealIndexName(index);
        return _utils.EnsureIndexAsync(normalized, vectorSize, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteIndexAsync(string index, CancellationToken cancellationToken = default)
    {
        string normalized = GetRealIndexName(index);
        await _utils.DeleteIndexAsync(normalized, cancellationToken);
    }

    private string GetRealIndexName(string index)
    {
        var indexName = $"{_config.IndexPrefix}{index}";
        var normalized = NormalizeIndexName(indexName);
        return normalized;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetIndexesAsync(CancellationToken cancellationToken = default)
    {
        var rawIndexes = await _utils.GetIndexesNamesAsync(cancellationToken);
        if (string.IsNullOrEmpty(_config.IndexPrefix))
        {
            return rawIndexes;
        }
        List<string> realIndexNames = new();
        foreach (var rawIndex in rawIndexes)
        {
            if (rawIndex.StartsWith(_config.IndexPrefix, StringComparison.OrdinalIgnoreCase))
            {
                realIndexNames.Add(rawIndex.Substring(_config.IndexPrefix.Length));
            }
        }

        return realIndexNames.AsReadOnly();
    }

    /// <inheritdoc />
    public Task DeleteAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        return _utils.DeleteRecordAsync(GetRealIndexName(index), record.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<MemoryRecord> GetListAsync(
        string index,
        ICollection<MemoryFilter>? filters = null,
        int limit = 1,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = ElasticSearchMemoryFilterConverter.CreateQueryDescriptorFromMemoryFilter(filters);

        if (limit <= 0)
        {
            limit = 10;
        }

        var resp = await _utils.QueryHelper.ExecuteQueryAsync(GetRealIndexName(index), limit, query, cancellationToken);

        foreach (var item in resp)
        {
            //source is a json element
            if (item.Source is JsonElement je)
            {
                yield return ElasticsearchMemoryRecord.MemoryRecordFromJsonElement(je, withEmbeddings);
            }
            else
            {
                _log.LogError("Received an answer from elastic where item.Source is not a JsonElement but is {type}", item.Source?.GetType()?.FullName ?? "null");
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(
        string index,
        string text,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = 1,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = ElasticSearchMemoryFilterConverter.CreateQueryDescriptorFromMemoryFilter(filters);

        if (limit <= 0)
        {
            limit = 10;
        }

        Embedding qembed = await _embeddingGenerator.GenerateEmbeddingAsync(text, cancellationToken).ConfigureAwait(false);
        var coll = qembed.Data.ToArray();

        //ok now we need to add a knn query to the elastic search
        var knnOuterQuery = new QueryDescriptor<object>().Knn(knn => knn
            .Filter(query)
            .Field("vector")
            .NumCandidates(limit * 2)
            .QueryVector(coll));

        var resp = await _utils.QueryHelper.ExecuteQueryAsync(GetRealIndexName(index), limit, knnOuterQuery, cancellationToken);

        foreach (var item in resp)
        {
            //source is a json element
            if (item.Source is JsonElement je)
            {
                var mr = ElasticsearchMemoryRecord.MemoryRecordFromJsonElement(je, withEmbeddings);
                //lets check if we can have cosine similarity direclty returned from the query for now we recalculate
                yield return (mr, item.Score ?? 0);
            }
            else
            {
                _log.LogError("Received an answer from elastic where item.Source is not a JsonElement but is {type}", item.Source?.GetType()?.FullName ?? "null");
            }
        }
    }

    public async IAsyncEnumerable<MemoryRecord> SearchKeywordAsync(
        string index,
        string query,
        ICollection<MemoryFilter>? filters = null,
        int limit = 1,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_config.IndexablePayloadProperties == null || _config.IndexablePayloadProperties.Length == 0)
        {
            yield break;
        }

        //Start creating a query with memory filter.
        var filterQuery = ElasticSearchMemoryFilterConverter.CreateQueryFromMemoryFilter(filters);

        if (limit <= 0)
        {
            limit = 10;
        }

        //we need to do a full text search, combined with the original query from the filter.

        //ok now we need to add a knn query to the elastic search
        var matchFields = _config.IndexablePayloadProperties.Select(f => $"txt_{f}").ToArray();
        var outerQuery = Query.Bool(new BoolQuery() 
        {
            Must = new Query[]
            {
                Query.MultiMatch(new MultiMatchQuery()
                {
                    Fields = matchFields,
                    Query = query
                }),
                filterQuery
            }
        });

        var resp = await _utils.QueryHelper.ExecuteQueryAsync(GetRealIndexName(index), limit, outerQuery, cancellationToken);

        foreach (var item in resp)
        {
            //source is a json element
            if (item.Source is JsonElement je)
            {
                var mr = ElasticsearchMemoryRecord.MemoryRecordFromJsonElement(je, withEmbeddings);
                //lets check if we can have cosine similarity direclty returned from the query for now we recalculate
                yield return mr;
            }
            else
            {
                _log.LogError("Received an answer from elastic where item.Source is not a JsonElement but is {type}", item.Source?.GetType()?.FullName ?? "null");
            }
        }
    }

    /// <inheritdoc />
    public async Task<string> UpsertAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(index))
        {
            throw new ArgumentException($"'{nameof(index)}' cannot be null or empty.", nameof(index));
        }

        var realIndexName = GetRealIndexName(index);
        await _utils.IndexMemoryRecordAsync(realIndexName, record, cancellationToken);
        return record.Id;
    }

    #region Bulk

    public async Task<IReadOnlyCollection<string>> UpsertManyAsync(
            string index,
            IEnumerable<MemoryRecord> records,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(index))
        {
            throw new ArgumentException($"'{nameof(index)}' cannot be null or empty.", nameof(index));
        }

        if (records is null)
        {
            throw new ArgumentNullException(nameof(records));
        }

        //now I need to bulk insert
        await _utils.BulkIndexMemoryRecordAsync(GetRealIndexName(index), records, cancellationToken);

        return records.Select(r => r.Id).ToArray();
    }

    public async Task<IReadOnlyCollection<string>> ReplaceDocumentAsync(string index, string documentId, IEnumerable<MemoryRecord> record, CancellationToken cancellationToken = default)
    {
        await _utils.DeleteByDocumentIdAsync(GetRealIndexName(index), documentId, cancellationToken);
        return await UpsertManyAsync(index, record, cancellationToken);
    }

    #endregion

    private static string NormalizeIndexName(string indexName)
    {
        if (string.IsNullOrWhiteSpace(indexName))
        {
            throw new ArgumentNullException(nameof(indexName), "The index name is empty");
        }

        return indexName.Replace("_", "-", StringComparison.OrdinalIgnoreCase);
    }
}
