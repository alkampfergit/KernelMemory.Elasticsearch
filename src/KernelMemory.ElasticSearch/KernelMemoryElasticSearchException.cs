using Microsoft.KernelMemory;
using System;

namespace KernelMemory.ElasticSearch;

internal class KernelMemoryElasticSearchException : KernelMemoryException
{
    /// <inheritdoc />
    public KernelMemoryElasticSearchException() { }

    /// <inheritdoc />
    public KernelMemoryElasticSearchException(string message) : base(message) { }

    /// <inheritdoc />
    public KernelMemoryElasticSearchException(string message, Exception? innerException) : base(message, innerException) { }
}
