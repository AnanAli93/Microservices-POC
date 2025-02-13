using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Bulkhead;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;
using System.Text.Json;
using System.Text;

namespace BusinessLogicLayer.Policies;

public class PollyPolicies : IPollyPolicies
{
    private readonly ILogger<PollyPolicies> _logger;
    public PollyPolicies(ILogger<PollyPolicies> logger)
    {
        _logger = logger;
    }


    public IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount)
    {
        AsyncRetryPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(retryCount: retryCount, sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), onRetry: (outcome, times, retryNumber, context) =>
                {
                    _logger.LogInformation($"Retry {retryNumber} after {times.TotalSeconds} seconds for {context.PolicyKey}");
                });
        return policy;
    }
    public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int handledEventsAllowedBeforeBreaking, TimeSpan durationOfBreak)
    {
        AsyncCircuitBreakerPolicy<HttpResponseMessage> policy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: handledEventsAllowedBeforeBreaking, durationOfBreak: durationOfBreak/*TimeSpan.FromMinutes(2)*/, onBreak: (outcome, times) =>
                {
                    _logger.LogInformation($"Circuit breaker opened for {times.TotalMinutes} Minutes due to consecutive 2 failures." +
                        $"the subsequent requests will be rejected.");
                }, onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker has been reset.");
                });
        return policy;
    }

    public IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeout)
    {
        AsyncTimeoutPolicy<HttpResponseMessage> policy = Policy.TimeoutAsync<HttpResponseMessage>(timeout/*TimeSpan.FromMilliseconds(1500)*/);
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

              var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
              {
                  Content = new StringContent(JsonSerializer.Serialize(product), Encoding.UTF8, "application/json")
              };

              return response;
          });

        return policy;
    }
    public IAsyncPolicy<HttpResponseMessage> GetBulkheadIsolationPolicy(int maxParallelization, int maxQueuingActions)
    {
        AsyncBulkheadPolicy<HttpResponseMessage> policy = Policy.BulkheadAsync<HttpResponseMessage>(
            maxParallelization: maxParallelization, //allows up to 2 requests at a time
            maxQueuingActions: maxQueuingActions, //queues up to 40 requests added to the queue
            onBulkheadRejectedAsync: (context) =>
            {
                _logger.LogWarning("Bulkhead triggered: The request failed, returning dummy data");
                throw new BulkheadRejectedException("Bulkhead triggered: The request failed, queue is full");
            }
            );
        return policy;
    }

}
