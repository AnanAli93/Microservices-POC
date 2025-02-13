using Polly;

namespace BusinessLogicLayer.Policies;

public interface IUserMicroservicePolicies
{
    IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy();
}
