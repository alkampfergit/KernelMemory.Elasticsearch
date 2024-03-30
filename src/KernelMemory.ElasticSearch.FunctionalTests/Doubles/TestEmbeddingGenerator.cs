using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;

namespace KernelMemory.ElasticSearch.FunctionalTests.Doubles
{
    public class TestEmbeddingGenerator : ITextEmbeddingGenerator
    {
        public int MaxTokens => 500;

        public int CountTokens(string text)
        {
            return text.Length;
        }

        public Task<Embedding> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            var hash = text.GetHashCode();
            return Task.FromResult(new Embedding(ExtractAndNormalizeBytes(hash)));
        }

        private static float[] ExtractAndNormalizeBytes(int input)
        {
            byte[] bytes = BitConverter.GetBytes(input);

            float b1 = (float)(bytes[0] - 128) / 128;
            float b2 = (float)(bytes[1] - 128) / 128;
            float b3 = (float)(bytes[2] - 128) / 128;
            float b4 = (float)(bytes[3] - 128) / 128;

            return [b1, b2, b3, b4];
        }
    }
}
