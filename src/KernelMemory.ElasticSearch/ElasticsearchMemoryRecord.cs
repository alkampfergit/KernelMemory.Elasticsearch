using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryStorage;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace KernelMemory.ElasticSearch;

/// <summary>
/// This is the class that allows converting from and to MemoryRecord objects to and from Elasticsearch indexable objects.  
/// </summary>
public static class ElasticsearchMemoryRecord
{
    public static IDictionary<string, object> ToIndexableObject(
        MemoryRecord memoryRecord,
        string[] indexablePayloadProperties)
    {
        var io = new ExpandoObject() as IDictionary<string, object>;
        io["id"] = memoryRecord.Id;

        if (memoryRecord.Tags != null)
        {
            foreach (var tag in memoryRecord.Tags)
            {
                io[$"tag_{tag.Key}"] = tag.Value;
            }
        }

        if (indexablePayloadProperties?.Length > 0) 
        {
            foreach (var prop in indexablePayloadProperties)
            {
                if (memoryRecord.Payload.ContainsKey(prop))
                {
                    var stringprop = memoryRecord.Payload[prop] as string;
                    if (stringprop != null)
                    {
                        io[$"txt_{prop}"] = stringprop;
                    }
                }
            }
        }

        io["payload"] = JsonSerializer.Serialize(memoryRecord.Payload);
        io["vector"] = memoryRecord.Vector.Data.ToArray();
        return io;
    }

    /// <summary>
    /// Create a memory record from an indexable object.
    /// </summary>
    public static MemoryRecord MemoryRecordFromIndexableObject(IDictionary<string, object> indexableObject, bool withEmbedding = true)
    {
        var mr = new MemoryRecord();
        mr.Id = indexableObject["id"] as string;
        var desDic = JsonSerializer.Deserialize<Dictionary<string, object>>(indexableObject["payload"] as string);
        mr.Payload = desDic.ToDictionary(
            kvp => kvp.Key,
            kvp => ConvertJsonElement(kvp.Value)
        );
        var tagKeys = indexableObject.Keys.Where(k => k.StartsWith("tag_"));
        foreach (var tagKey in tagKeys)
        {
            mr.Tags[tagKey.Substring(4)] = indexableObject[tagKey] as List<string>;
        }
        if (withEmbedding)
        {
            mr.Vector = new Embedding(indexableObject["vector"] as float[]);
        }
        return mr;
    }

    /// <summary>
    /// Create a memory record from a Json object.
    /// </summary>
    public static MemoryRecord MemoryRecordFromJsonElement(JsonElement jsonElement, bool withEmbedding = true)
    {
        var mr = new MemoryRecord();
        mr.Id = jsonElement.GetProperty("id").GetString();

        var eo = jsonElement.EnumerateObject();
        var tagsProperties = eo.Where(eo => eo.Name.StartsWith("tag_"));
        foreach (var tagProperty in tagsProperties)
        {
            mr.Tags[tagProperty.Name.Substring(4)] = tagProperty.Value.EnumerateArray().Select(jv => jv.GetString()).ToList();
        }

        //now get the payload
        var desDic = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetProperty("payload").GetString());
        mr.Payload = desDic.ToDictionary(
            kvp => kvp.Key,
            kvp => ConvertJsonElement(kvp.Value)
        );

        if (withEmbedding)
         {
            mr.Vector = new Embedding(jsonElement.GetProperty("vector").EnumerateArray().Select(jv => jv.GetSingle()).ToArray());
        }

        return mr;
    }

    private static object ConvertJsonElement(object element)
    {
        if (!(element is JsonElement jsonElement))
        {
            return element;
        }

        switch (jsonElement.ValueKind)
        {
            case JsonValueKind.String:
                return jsonElement.GetString();
            case JsonValueKind.Number:
                return jsonElement.GetDouble(); // or GetInt32(), GetInt64(), etc. depending on the expected number type
            case JsonValueKind.True:
            case JsonValueKind.False:
                return jsonElement.GetBoolean();
                // handle other types as needed
        }

        return element;
    }
}
