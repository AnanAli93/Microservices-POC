

using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BusinessLogicLayer.HttpClients;

public class UserMicroserviceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserMicroserviceClient> _logger;
    private readonly IDistributedCache _distributedCache;
    public UserMicroserviceClient(HttpClient httpClient, ILogger<UserMicroserviceClient> logger, IDistributedCache distributedCache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _distributedCache = distributedCache;
    }

    public async Task<UserDTO?> GetUserByUserID(Guid userId)
    {
        try
        {
            var key = $"user:{userId}";
            var cachedUser = await _distributedCache.GetStringAsync(key);
            if (cachedUser != null)
                return JsonSerializer.Deserialize<UserDTO>(cachedUser);
            
            HttpResponseMessage response = await _httpClient.GetAsync($"/gateway/users/{userId}");
            if (!response.IsSuccessStatusCode)
            {
                if(response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    UserDTO? userFromFullbackPolicy = await response.Content.ReadFromJsonAsync<UserDTO>();
                    if (userFromFullbackPolicy == null)
                    {
                        throw new NotImplementedException("Fullback policy failed");
                    }
                    return userFromFullbackPolicy;
                }
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    throw new HttpRequestException("Bad Request", null, HttpStatusCode.NotFound);
                else
                    return new UserDTO(PersonName: "Error", Email: "Error", Gender: "Error", UserID: Guid.Empty);
            }
            else
            {
                var user = await response.Content.ReadFromJsonAsync<UserDTO>();
                if (user == null)
                    throw new ArgumentException("User not found");
                string cacheKey = $"user:{userId}";
                var options = new DistributedCacheEntryOptions()
                                  .SetAbsoluteExpiration(DateTimeOffset.UtcNow.AddMinutes(5))
                                  .SetSlidingExpiration(TimeSpan.FromMinutes(3));
                await _distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(user), options);
                return user;
            }
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, " Request failed due to Circuit breaker opened");
            return new UserDTO(PersonName: "Error", Email: "Error", Gender: "Error", UserID: Guid.Empty);
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogError(ex, "Request failed due to timeout");
            return new UserDTO(PersonName: "Error Timeout", Email: "Error Timeout", Gender: "Error Timeout", UserID: Guid.Empty);
        }

    }
}
