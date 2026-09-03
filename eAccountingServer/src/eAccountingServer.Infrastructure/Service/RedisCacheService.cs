using System.Text.Json;
using eAccountingServer.Application.Services;
using StackExchange.Redis;

namespace eAccountingServer.Infrastructure.Service;
internal sealed class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _redis;
    private readonly CacheKeyScope _keyScope;

    public RedisCacheService(IConnectionMultiplexer redis, CacheKeyScope keyScope)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _keyScope = keyScope;
    }

    public T? Get<T>(string key)
    {
        var value = _database.StringGet(_keyScope.Qualify(key));
        if (value.HasValue)
            return JsonSerializer.Deserialize<T>(value.ToString());

        return default(T?);
    }

    public bool Remove(string key)
    {
        // Mirrors MemoryCacheService: invalidate the key for every tenant that holds it.
        bool removed = false;

        foreach (var endPoint in _redis.GetEndPoints())
        {
            IServer server = _redis.GetServer(endPoint);
            if (!server.IsConnected || server.IsReplica) continue;

            foreach (var redisKey in server.Keys(_database.Database, pattern: $"*{CacheKeyScope.Suffix(key)}"))
                removed |= _database.KeyDelete(redisKey);
        }

        return removed;
    }

    public void Set<T>(string key, T value, TimeSpan? expiry = null)
    {
        var serializedValue = JsonSerializer.Serialize(value);
        _database.StringSet(_keyScope.Qualify(key), serializedValue, expiry ?? TimeSpan.FromHours(1));
    }
}
