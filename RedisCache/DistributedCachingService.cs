using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using RedisCluster.Interfaces;
using RedisCluster.Interfaces.Converters;
using RedisCluster.Models;
using System;

namespace RedisCluster
{
    public class DistributedCachingService : ICachingService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly IStringConverter _converter;
        private const int MEMORY_TTL_SECONDS = 10;
        private const int DISTRIBUTED_TTL_SECONDS = 30;
        private CircuitBreaker _writeCircuitBreaker = new CircuitBreaker();
        private CircuitBreaker _readCircuitBreaker = new CircuitBreaker();

        public DistributedCachingService(IDistributedCache distributedCache, IMemoryCache memoryCache, IStringConverter converter)
        {
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
            _converter = converter;
        }

        public T GetOrCreate<T>(string key, Func<T> factory)
        {
            var local = _memoryCache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpiration = DateTime.UtcNow.AddSeconds(MEMORY_TTL_SECONDS);
                return GetFromDistributedCache(key, factory);
            });

            return _converter.Deserialize<CacheWrapper<T>>(local).Data;
        }

        private string GetFromDistributedCache<T>(string key, Func<T> factory)
        {
            bool readSucceeded = true;
            try
            {
                string cachedItem = string.Empty;
                _readCircuitBreaker.ExecuteAction(() =>
                {
                    cachedItem = _distributedCache.GetString(key);
                });
                
                if (cachedItem != null)
                {
                    return cachedItem;
                }
            }
            catch (Exception ex)
            {
                readSucceeded = false;
            }

            var item = factory.Invoke();
            var cacheItem = _converter.Serialize(new CacheWrapper<T>(item));

            try
            {
                // if the read circuit is broken then writes will also be broken
                if (readSucceeded && _readCircuitBreaker.IsClosed)
                {
                    var cacheEntryOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(DISTRIBUTED_TTL_SECONDS) };
                    _writeCircuitBreaker.ExecuteAction(() =>
                    {
                        _distributedCache.SetString(key, cacheItem, cacheEntryOptions);
                    });
                }
            }
            catch (Exception ex)
            {
                // log exception here
            }

            return cacheItem;
        }
    }
}
