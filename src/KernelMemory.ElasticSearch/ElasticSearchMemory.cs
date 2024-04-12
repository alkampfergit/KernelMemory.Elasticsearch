using KernelMemory.ElasticSearch;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.MemoryStorage;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.KernelMemory.MongoDbAtlas;

/// <summary>
/// Implementation of <see cref="IMemoryDb"/> based on MongoDB Atlas.
/// </summary>
public class ElasticSearchMemory : IMemoryDb
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
        this._embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
        this._log = log ?? DefaultLogger<ElasticSearchMemory>.Instance;
        this._config = config;
        this._utils = new ElasticSearchHelper(this._config);
    }

    /// <inheritdoc />
    public Task CreateIndexAsync(string index, int vectorSize, CancellationToken cancellationToken = default)
    {
        string normalized = GetRealIndexName(index);
        return this._utils.EnsureIndexAsync(normalized, vectorSize, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteIndexAsync(string index, CancellationToken cancellationToken = default)
    {
        string normalized = GetRealIndexName(index);
        await this._utils.DeleteIndexAsync(normalized, cancellationToken); 
    }

    private string GetRealIndexName(string index)
    {
        var indexName = $"{this._config.IndexPrefix}{index}";
        var normalized = NormalizeIndexName(indexName);
        return normalized;
    }

    /// <inheritdoc />
    public Task<IEnumerable<string>> GetIndexesAsync(CancellationToken cancellationToken = default)
    {
        return _utils.GetIndexesNamesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
        //var collection = this.GetCollectionFromIndexName(index);
        //return collection.DeleteOneAsync(x => x.Id == record.Id, cancellationToken: cancellationToken);
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
    public IAsyncEnumerable<(MemoryRecord, double)> GetSimilarListAsync(
        string index,
        string text,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = 1,
        bool withEmbeddings = false,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();

        //if (limit <= 0)
        //{
        //    limit = 10;
        //}

        //// Need to create a search query and execute it
        //var collectionName = this.GetCollectionName(index);
        //var embeddings = await this._embeddingGenerator.GenerateEmbeddingAsync(text, cancellationToken).ConfigureAwait(false);

        //// Define vector embeddings to search
        //var vector = embeddings.Data.Span.ToArray();

        //// Need to create the filters
        //var finalFilter = this.TranslateFilters(filters, index);

        //var options = new VectorSearchOptions<MongoDbAtlasMemoryRecord>()
        //{
        //    IndexName = this._utils.GetIndexName(collectionName),
        //    NumberOfCandidates = limit,
        //    Filter = finalFilter
        //};
        //var collection = this.GetCollectionFromIndexName(index);

        //// Run query
        //var documents = await collection.Aggregate()
        //    .VectorSearch(m => m.Embedding, vector, limit, options)
        //    .ToListAsync(cancellationToken).ConfigureAwait(false);

        //// If you check documentation Atlas normalize the score with formula
        //// score = (1 + cosine/dot_product(v1,v2)) / 2
        //// Thus it does not output the real cosine similarity, this is annoying so we
        //// need to recompute cosine similarity using the embeddings.
        //foreach (var document in documents)
        //{
        //    var memoryRecord = FromMongodbMemoryRecord(document, withEmbeddings);

        //    // we have score that is normalized, so we need to recompute similarity to have a real cosine distance
        //    var cosineSimilarity = CosineSim(embeddings, document.Embedding);
        //    if (cosineSimilarity < minRelevance)
        //    {
        //        //we have reached the limit for minimum relevance so we can stop iterating
        //        break;
        //    }

        //    yield return (memoryRecord, cosineSimilarity);
        //}
    }

    /// <inheritdoc />
    public Task<string> UpsertAsync(string index, MemoryRecord record, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
        //var normalizedIndexName = NormalizeIndexName(index);
        //var collection = this.GetCollectionFromIndexName(index);
        //MongoDbAtlasMemoryRecord mongoRecord = new()
        //{
        //    Id = record.Id,
        //    Index = normalizedIndexName,
        //    Embedding = record.Vector.Data.ToArray(),
        //    Tags = record.Tags.Select(x => new MongoDbAtlasMemoryRecord.Tag(x.Key, x.Value.ToArray())).ToList(),
        //    Payloads = record.Payload.Select(x => new MongoDbAtlasMemoryRecord.Payload(x.Key, x.Value)).ToList()
        //};

        //await collection.InsertOneAsync(mongoRecord, cancellationToken: cancellationToken).ConfigureAwait(false);
        //await this.Config.AfterIndexCallbackAsync().ConfigureAwait(false);
        //return record.Id;
    }

    private static string NormalizeIndexName(string indexName)
    {
        if (string.IsNullOrWhiteSpace(indexName))
        {
            throw new ArgumentNullException(nameof(indexName), "The index name is empty");
        }

        return indexName.Replace("_", "", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Due to different score system of MongoDB Atlas that normalized cosine
    /// we need to manually recompute the cosine similarity distance manually
    /// for each vector to have a real cosine similarity distance returned.
    /// </summary>
    private static double CosineSim(Embedding vec1, float[] vec2)
    {
        var v1 = vec1.Data.ToArray();
        var v2 = vec2;

        int size = vec1.Length;
        double dot = 0.0d;
        double m1 = 0.0d;
        double m2 = 0.0d;
        for (int n = 0; n < size; n++)
        {
            dot += v1[n] * v2[n];
            m1 += Math.Pow(v1[n], 2);
            m2 += Math.Pow(v2[n], 2);
        }

        double cosineSimilarity = dot / (Math.Sqrt(m1) * Math.Sqrt(m2));
        return cosineSimilarity;
    }
}
