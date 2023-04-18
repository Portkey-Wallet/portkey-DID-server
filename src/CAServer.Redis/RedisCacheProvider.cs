using CAServer.Cache;
using StackExchange.Redis;
using Volo.Abp.DependencyInjection;

namespace CAServer.Redis;

public class RedisCacheProvider : ICacheProvider, ISingletonDependency
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;

    public RedisCacheProvider(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _database = _connectionMultiplexer.GetDatabase();
    }

    public async Task HSetWithExpire(string key, string member, string value, TimeSpan? expiry)
    {
        _database.HashSet(key, member, value);
        _database.KeyExpire(key, expiry);
    }

    public async Task<bool> HashDelete(string key, string member)
    {
        return _database.HashDelete(key, member);
    }

    public async Task<HashEntry[]> HGetAll(string key)
    {
        return await _database.HashGetAllAsync(key);
    }

    public async Task Set(string key, string value, TimeSpan? expiry)
    {
        await _database.StringSetAsync(key, value);
        _database.KeyExpire(key, expiry);
    }

    public async Task<RedisValue> Get(string key)
    {
        return await _database.StringGetAsync(key);
    }

    public async Task Delete(string key)
    {
        _database.KeyDelete(key);
    }

    public async Task<Dictionary<string, RedisValue>> BatchGet(List<string> keys)
    {
        var batch = _database.CreateBatch();
        var tmpAns = new Dictionary<string, Task<RedisValue>>(keys.Count);
        foreach (var key in keys)
        {
            tmpAns[key] = batch.StringGetAsync(key);
        }

        batch.Execute();
        var realAns = new Dictionary<string, RedisValue>(keys.Count);
        foreach (var kv in tmpAns.Where(kv => kv.Value != null))
        {
            realAns[kv.Key] = kv.Value.Result;
        }

        return realAns;
    }
}