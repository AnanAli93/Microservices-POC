using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;

namespace BusinessLogicLayer.Policies;

public class UserMicroservicePolicy : IUserMicroservicePolicies
{
    private readonly IPollyPolicies _pollyPolicies;
    private readonly ILogger<UserMicroservicePolicy> _logger;
    public UserMicroservicePolicy(ILogger<UserMicroservicePolicy> logger, IPollyPolicies pollyPolicies)
    {
        _logger = logger;
        _pollyPolicies = pollyPolicies;

    }
    public IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy()
    {
        var retryPolicy = _pollyPolicies.GetRetryPolicy(4);
        var circuitBreakerPolicy = _pollyPolicies.GetCircuitBreakerPolicy(2, TimeSpan.FromMinutes(2));
        var timeoutPolicy = _pollyPolicies.GetTimeoutPolicy(TimeSpan.FromMilliseconds(1500));
        AsyncPolicyWrap<HttpResponseMessage> combinedPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy, timeoutPolicy);
        //retryPolicy.WrapAsync(circuitBreakerPolicy).WrapAsync(timeoutPolicy);
        return combinedPolicy;
    }
}
