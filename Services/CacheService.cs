using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace GameStore.Api.Services;

//interface for cache service
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
}

// interface implementation for cache service
public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;

    // constructor for cache service
    public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    // get object from cache by key
    public async Task<T?> GetAsync<T>(string key)
    {
        var data = await _cache.GetStringAsync(key);    
        if (data == null) return default;

        return JsonSerializer.Deserialize<T>(data);
    }

    // Write object to cache with key and optional expiration time
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
        };

        var data = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, data, options);
        
        _logger.LogInformation("Cached: {Key}", key);
    }

    // Delete object from cache by key
    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
        _logger.LogInformation("Cache removed: {Key}", key);
    }

    // Get object from cache or create it if it doesn't exist
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        try
        {
            var cached = await GetAsync<T>(key);
            if (cached != null)
            {
                _logger.LogInformation("Cache hit: {Key}", key);
                return cached;
            }

            _logger.LogInformation("Cache miss: {Key}, fetching from source", key);
            var value = await factory();
            
            try
            {
                await SetAsync(key, value, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to cache {Key}: {Error}", key, ex.Message);
            }
            
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Cache error for {Key}: {Error}. Executing factory directly.", key, ex.Message);
            return await factory();
        }
    }
}