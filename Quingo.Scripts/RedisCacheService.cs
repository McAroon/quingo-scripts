using System.Text.Json;
using StackExchange.Redis;

namespace Quingo.Scripts;

public class RedisCacheService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }
    
    private static RedisKey RedisKey(string key) => $"quingo:scripts:{key}";

    public async Task<T> Get<T>(string key)
    {
        var db = _redis.GetDatabase();
        var json = await db.StringGetAsync(RedisKey(key));
        return json.HasValue ? JsonSerializer.Deserialize<T>(json) : default;
    }

    public async Task Set<T>(string key, T data)
    {
        var db = _redis.GetDatabase();
        var json = JsonSerializer.Serialize(data);
        await db.StringSetAsync(RedisKey(key), json);
    }

    public async Task Remove(string key)
    {
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(RedisKey(key));
    }
    
    public async Task RemoveAll()
    {
        var prepared = LuaScript.Prepare("for i, name in ipairs(redis.call('KEYS', @prefix)) do redis.call('DEL', name); end");
        await _redis.GetDatabase().ScriptEvaluateAsync(prepared, new { prefix = RedisKey("*") });
    }
}