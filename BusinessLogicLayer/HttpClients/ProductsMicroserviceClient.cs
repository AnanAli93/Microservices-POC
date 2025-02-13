using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Polly.Bulkhead;
using System.Net.Http.Json;
using System.Text.Json;

namespace BusinessLogicLayer.HttpClients;

public class ProductsMicroserviceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductsMicroserviceClient> _logger;
    private readonly IDistributedCache _distributedCache;

    public ProductsMicroserviceClient(HttpClient httpClient, ILogger<ProductsMicroserviceClient> logger, IDistributedCache distributedCache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _distributedCache = distributedCache;
    }


    public async Task<ProductDTO?> GetProductByProductID(Guid productID)
    {
        try
        {
            //key:product:123
            //value:ProductDTO
            string cacheKey = $"product:{productID}";
            var cachedProduct = await _distributedCache.GetStringAsync(cacheKey);
            if (cachedProduct != null)
            {
                var productFromCache = JsonSerializer.Deserialize<ProductDTO>(cachedProduct);
                return productFromCache;
            }
            HttpResponseMessage response = await _httpClient.GetAsync($"/gateway/products/search/product-id/{productID}");

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    ProductDTO? productFromFullbackPolicy = await response.Content.ReadFromJsonAsync<ProductDTO>();
                    if (productFromFullbackPolicy == null)
                    {
                        throw new NotImplementedException("Fullback policy failed");
                    }
                    return productFromFullbackPolicy;
                }
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    throw new HttpRequestException("Bad request", null, System.Net.HttpStatusCode.BadRequest);
                }
                else
                {
                    throw new HttpRequestException($"Http request failed with status code {response.StatusCode}");
                }
            }


            ProductDTO? product = await response.Content.ReadFromJsonAsync<ProductDTO>();

            if (product == null)
            {
                throw new ArgumentException("Invalid Product ID");
            }
            string serializedProduct = JsonSerializer.Serialize(product);
            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(300))
                .SetSlidingExpiration(TimeSpan.FromSeconds(100));
            string cacheKeyToWrite = $"product:{product.ProductID}";
            await _distributedCache.SetStringAsync(cacheKeyToWrite, serializedProduct, options);
            return product;
        }
        catch (BulkheadRejectedException ex)
        {
            _logger.LogError(ex, "Request failed due to bulkhead limit reached");
            return new ProductDTO(ProductID: Guid.Empty, ProductName: "Error bulkhead limit", Category: "Error bulkhead limit", UnitPrice: 0, QuantityInStock: 0);
        }

    }
}

