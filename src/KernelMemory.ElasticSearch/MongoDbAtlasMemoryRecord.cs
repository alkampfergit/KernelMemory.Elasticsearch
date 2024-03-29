//// Copyright (c) Microsoft. All rights reserved.

//using Microsoft.KernelMemory.MemoryStorage;
//using System.Collections.Generic;
//using System.Text.Json.Serialization;

//namespace Microsoft.KernelMemory.MongoDbAtlas;

///// <summary>
///// This is the class that represents a memory record in the ElasticSearch database.
///// </summary>
//public sealed class ElasticsearchMemoryRecord
//{
//    private const string PayloadField = "payload";
//    private const string ContentField = "content";

//    /// <summary>
//    /// TBC
//    /// </summary>
//    [JsonPropertyName("Id")]
//    public string Id { get; set; } = string.Empty;

//    /// <summary>
//    /// TBC
//    /// </summary>
//    [JsonPropertyName(TagsField)]
//    public List<ElasticsearchTag> Tags { get; set; } = new();

//    /// <summary>
//    /// TBC
//    /// </summary>
//    [JsonPropertyName(PayloadField)]
//    public string Payload { get; set; } = string.Empty;

//    /// <summary>
//    /// TBC
//    /// </summary>
//    [JsonPropertyName(ContentField)]
//    public string Content { get; set; } = string.Empty;

//    /// <summary>
//    /// TBC
//    /// </summary>
//    [JsonPropertyName(EmbeddingField)]
//    [JsonConverter(typeof(Embedding.JsonConverter))]
//    public Embedding Vector { get; set; } = new();

//    /// <summary>
//    /// TBC
//    /// </summary>
//    public MemoryRecord ToMemoryRecord(bool withEmbedding = true)
//    {
//        MemoryRecord result = new()
//        {
//            Id = this.Id,
//            Payload = JsonSerializer.Deserialize<Dictionary<string, object>>(this.Payload, s_jsonOptions)
//                      ?? new Dictionary<string, object>()
//        };
//        // TODO: remove magic string
//        result.Payload["text"] = this.Content;

//        if (withEmbedding)
//        {
//            result.Vector = this.Vector;
//        }

//        foreach (var tag in this.Tags)
//        {
//            result.Tags.Add(tag.Name, tag.Value);
//        }

//        return result;
//    }

//    /// <summary>
//    /// TBC
//    /// </summary>
//    /// <param name="record"></param>
//    /// <returns></returns>
//    public static ElasticsearchMemoryRecord FromMemoryRecord(MemoryRecord record)
//    {
//        ArgumentNullException.ThrowIfNull(record);

//        // TODO: remove magic strings
//        string content = record.Payload["text"]?.ToString() ?? string.Empty;
//        string documentId = record.Tags["__document_id"][0] ?? string.Empty;
//        string filePart = record.Tags["__file_part"][0] ?? string.Empty;
//        string betterId = $"{documentId}|{filePart}";

//        record.Payload.Remove("text"); // We move the text to the content field. No need to index twice.

//        ElasticsearchMemoryRecord result = new()
//        {
//            Id = record.Id,
//            Vector = record.Vector,
//            Payload = JsonSerializer.Serialize(record.Payload, s_jsonOptions),
//            Content = content
//        };

//        foreach (var tag in record.Tags)
//        {
//            if ((tag.Value == null) || (tag.Value.Count == 0))
//            {
//                // Key only, with no values
//                result.Tags.Add(new ElasticsearchTag(name: tag.Key));
//                continue;
//            }

//            foreach (var value in tag.Value)
//            {
//                // Key with one or more values
//                result.Tags.Add(new ElasticsearchTag(name: tag.Key, value: value));
//            }
//        }

//        return result;
//    }
//}
