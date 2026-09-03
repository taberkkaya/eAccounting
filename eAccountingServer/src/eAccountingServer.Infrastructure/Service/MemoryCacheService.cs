using System.Collections.Concurrent;
using eAccountingServer.Application.Services;
using Microsoft.Extensions.Caching.Memory;

namespace eAccountingServer.Infrastructure.Service;
internal class MemoryCacheService(
    IMemoryCache cache,
    CacheKeyScope keyScope
    ) : ICacheService
{
    // IMemoryCache cannot enumerate its own keys, so the tenant-qualified keys that
    // have been issued are tracked here to make cross-tenant invalidation possible.
    private static readonly ConcurrentDictionary<string, byte> IssuedKeys = new();

    public T? Get<T>(string key)
    {
        cache.TryGetValue<T>(keyScope.Qualify(key), out var value);
        return value;
    }

    public bool Remove(string key)
    {
        // A write by one company can invalidate a list every company caches under the
        // same plain key (users, companies), so drop the key for all of them.
        string suffix = CacheKeyScope.Suffix(key);

        foreach (var issuedKey in IssuedKeys.Keys)
        {
            if (!issuedKey.EndsWith(suffix, StringComparison.Ordinal)) continue;

            cache.Remove(issuedKey);
            IssuedKeys.TryRemove(issuedKey, out _);
        }

        return true;
    }

    public void Set<T>(string key, T value, TimeSpan? expiry = null)
    {
        string qualifiedKey = keyScope.Qualify(key);

        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromHours(1),
        };

        cache.Set<T>(qualifiedKey, value, cacheEntryOptions);
        IssuedKeys.TryAdd(qualifiedKey, 0);
    }
}
