using Microsoft.KernelMemory;
using Microsoft.KernelMemory.MemoryStorage;
using System.Collections.Generic;
using System.Threading;

namespace KernelMemory.ElasticSearch
{
    /// <summary>
    /// Some advanced functinoalities that are useful to query the memory database
    /// not only with pure vector search but also with text search or mixed.
    /// </summary>
    public interface IAdvancedMemoryDb
    {
        IAsyncEnumerable<MemoryRecord> SearchKeywordAsync(
          string index,
          string query,
          ICollection<MemoryFilter>? filters = null,
          int limit = 1,
          bool withEmbeddings = false,
          CancellationToken cancellationToken = default);
    }
}
