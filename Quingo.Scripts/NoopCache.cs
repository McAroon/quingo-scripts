using Quingo.Infrastructure;

namespace Quingo.Scripts;

public class NoopCache : ICacheService
{
    public T Get<T>(string key)
    {
        return default;
    }

    public void Set<T>(string key, T data)
    {
        
    }

    public void Remove(string key)
    {
        
    }
}