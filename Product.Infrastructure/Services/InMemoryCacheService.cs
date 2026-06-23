using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Product.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Product.Infrastructure.Services
{
    public class InMemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<InMemoryCacheService> _logger;

        // Track keys by prefix so we can bulk-invalidate (e.g. "products:*")
        private readonly Dictionary<string, HashSet<string>> _prefixIndex = new();
        private readonly SemaphoreSlim _lock = new(1, 1);

        public InMemoryCacheService(IMemoryCache cache, ILogger<InMemoryCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
        {
            var value = _cache.TryGetValue(key, out T? cached) ? cached : null;
            _logger.LogDebug("[Cache] {Result} key={Key}", value is null ? "MISS" : "HIT", key);
            return Task.FromResult(value);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(5)
            };

            _cache.Set(key, value, options);
            _logger.LogDebug("[Cache] SET key={Key} ttl={TTL}", key, options.AbsoluteExpirationRelativeToNow);

            // Index key under its prefix (everything before the first ":")
            var prefix = key.Contains(':') ? key[..key.IndexOf(':')] : key;
            await _lock.WaitAsync(ct);
            try
            {
                if (!_prefixIndex.TryGetValue(prefix, out var keys))
                    _prefixIndex[prefix] = keys = new HashSet<string>();
                keys.Add(key);
            }
            finally { _lock.Release(); }
        }

        public Task RemoveAsync(string key, CancellationToken ct = default)
        {
            _cache.Remove(key);
            _logger.LogDebug("[Cache] REMOVE key={Key}", key);
            return Task.CompletedTask;
        }

        public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
        {
            await _lock.WaitAsync(ct);
            try
            {
                if (_prefixIndex.TryGetValue(prefix, out var keys))
                {
                    foreach (var k in keys) _cache.Remove(k);
                    _logger.LogDebug("[Cache] REMOVE_PREFIX prefix={Prefix} count={Count}", prefix, keys.Count);
                    _prefixIndex.Remove(prefix);
                }
            }
            finally { _lock.Release(); }
        }
    }

}
