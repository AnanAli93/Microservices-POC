using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Bulkhead;
using Polly.Fallback;
using System.Text;
using System.Text.Json;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies;

public class ProductsMicroservicePolicies : IProductsMicroservicePolicies
{
    private readonly ILogger<ProductsMicroservicePolicies> _logger;

    public ProductsMicroservicePolicies(ILogger<ProductsMicroservicePolicies> logger)
    {
        _logger = logger;
    }

    public IAsyncPolicy<HttpResponseMessage> GetBulkheadIsolationPolicy()
    {
        AsyncBulkheadPolicy<HttpResponseMessage> policy = Policy.BulkheadAsync<HttpResponseMessage>(
            maxParallelization: 10, //allows up to 2 requests at a time
            maxQueuingActions: 40, //queues up to 40 requests added to the queue
            onBulkheadRejectedAsync: (context) =>
            {
                _logger.LogWarning("Bulkhead triggered: The request failed, returning dummy data");
                throw new BulkheadRejectedException("Bulkhead triggered: The request failed, queue is full");
            }
            );
        return policy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy()
    {
        AsyncFallbackPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
          .FallbackAsync(async (context) =>
          {
              _logger.LogWarning("Fallback triggered: The request failed, returning dummy data");

              ProductDTO product = new ProductDTO(ProductID: Guid.Empty,
            ProductName: "Temporarily Unavailable (fallback)",
            Category: "Temporarily Unavailable (fallback)",
            UnitPrice: 0,
            QuantityInStock: 0
            );

              var response = new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
              {
                  Content = new StringContent(JsonSerializer.Serialize(product), Encoding.UTF8, "application/json")
              };

              return response;
          });

        return policy;
    }
}
