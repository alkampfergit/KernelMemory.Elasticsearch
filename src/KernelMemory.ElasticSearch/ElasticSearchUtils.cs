using Elastic.Transport.Products.Elasticsearch;

namespace KernelMemory.ElasticSearch
{
    internal static class ElasticSearchUtils
    {
        internal static string GetErrorFromElasticResponse(this ElasticsearchResponse elasticsearchResponse)
        {
            if (elasticsearchResponse.ElasticsearchServerError != null)
            {
                return $"{elasticsearchResponse.ElasticsearchServerError.Error}";
            }
            if (elasticsearchResponse.ApiCallDetails?.OriginalException != null)
            {
                return elasticsearchResponse.ApiCallDetails.OriginalException.ToString();
            }

            return "Generic error";
        }
    }
}
