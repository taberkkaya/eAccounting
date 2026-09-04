namespace eAccountingServer.Application.Services;
public interface ICacheService
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan? expiry = null);
    bool Remove(string key);

    /// <summary>
    /// Drops everything cached for one tenant. Needed when a tenant's data changes
    /// underneath the cache rather than through it - a demo sandbox being wiped and
    /// reseeded, for instance.
    /// </summary>
    void RemoveTenant(string tenantId);
}
