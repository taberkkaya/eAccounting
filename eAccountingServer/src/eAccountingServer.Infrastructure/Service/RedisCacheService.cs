using System.Text.Json;
using eAccountingServer.Application.Services;
using StackExchange.Redis;

namespace eAccountingServer.Infrastructure.Service;
internal sealed class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public T? Get<T>(string key)
    {
        var value = _database.StringGet(key);
        if (value.HasValue)
            return JsonSerializer.Deserialize<T>(value.ToString());

        return default(T?);
    }

    public bool Remove(string key)
    {
        return _database.KeyDelete(key);
    }

    public void Set<T>(string key, T value, TimeSpan? expiry = null)
    {
        var serializedValue = JsonSerializer.Serialize(value);
        _database.StringSet(key, serializedValue, expiry);
    }
}
