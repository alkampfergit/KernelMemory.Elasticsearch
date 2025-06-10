using Elastic.Transport.Products.Elasticsearch;
using System.Text;

namespace KernelMemory.ElasticSearch
{
    internal static class ElasticSearchUtils
    {
        internal static string GetErrorFromElasticResponse(this ElasticsearchResponse elasticsearchResponse)
        {
            StringBuilder errors = new();
            if (elasticsearchResponse.ElasticsearchServerError != null)
            {
                errors.AppendLine($"ElasticsearchServerError: {elasticsearchResponse.ElasticsearchServerError.Error}");
            }

            if (elasticsearchResponse.ApiCallDetails != null)
            {
                errors.AppendLine($"ApiResponseCode: {elasticsearchResponse.ApiCallDetails.HttpStatusCode}");
                if (elasticsearchResponse.ApiCallDetails?.OriginalException != null)
                {
                    errors.AppendLine($"ApiCallException: {elasticsearchResponse.ApiCallDetails.OriginalException.ToString()}");
                }
                errors.AppendLine($"ApiResponseDebugInfo: {elasticsearchResponse.ApiCallDetails.DebugInformation}");
            }

            if (errors.Length == 0) return "Generic error";

            return errors.ToString();
        }
    }
}
