using MarbleCraftOMS.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MarbleCraftOMS.Infrastructure.Services;

public class MemoryCacheAdapter<T>(IMemoryCache cache) : ICache<T>
{
    public bool TryGet(string key, out T? value) =>
        cache.TryGetValue(key, out value);

    public void Set(string key, T value, TimeSpan expiry) =>
        cache.Set(key, value, expiry);
}
