using Microsoft.KernelMemory;
using System.Collections.Generic;

namespace KernelMemory.ElasticSearch;

public class ExtendedMemoryFilter : MemoryFilter
{
    /// <summary>
    /// This collection of tags contains all the tags that are used to
    /// negatively filter out memory records.
    /// </summary>
    private TagCollection _notTags = new();

    public MemoryFilter ByNotTag(string name, string value)
    {
        this._notTags.Add(name, value);
        return this;
    }

    /// <summary>
    /// Gets all the filters that needs to be put as not into  the query
    /// </summary>
    /// <returns></returns>
    public IEnumerable<KeyValuePair<string, string?>> GetNotFilters()
    {
        return this._notTags.ToKeyValueList();
    }
}
