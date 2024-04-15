using Microsoft.KernelMemory;

namespace KernelMemory.ElasticSearch.FunctionalTests
{
    internal static class TestUtils
    {
        /// <summary>
        /// Due to different score system of MongoDB Atlas that normalized cosine
        /// we need to manually recompute the cosine similarity distance manually
        /// for each vector to have a real cosine similarity distance returned.
        /// </summary>
        internal static double CosineSim(Embedding vec1, float[] vec2)
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
}
