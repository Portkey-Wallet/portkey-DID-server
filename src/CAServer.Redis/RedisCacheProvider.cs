using CAServer.Cache;
using Newtonsoft.Json;
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

    public async Task HSetWithExpire(string key, string member, string value, TimeSpan? expire)
    {
        _database.HashSet(key, member, value);
        _database.KeyExpire(key, expire);
    }

    public async Task<bool> HashDeleteAsync(string key, string member)
    {
        return _database.HashDelete(key, member);
    }

    public async Task<HashEntry[]> HGetAll(string key)
    {
        return await _database.HashGetAllAsync(key);
    }

    public async Task Set(string key, string value, TimeSpan? expire)
    {
        await _database.StringSetAsync(key, value, expiry: expire);
        //_database.KeyExpire(key, expire);
    }

    /// <summary>
    /// T can not be null
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expire"></param>
    /// <typeparam name="T"></typeparam>
    public async Task Set<T>(string key, T? value, TimeSpan? expire) where T : class
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value), "redis cache set error, value can not be null.");
        }

        await _database.StringSetAsync(key, JsonConvert.SerializeObject(value), expiry: expire);
    }

    public async Task<RedisValue> Get(string key)
    {
        return await _database.StringGetAsync(key);
    }

    public async Task<T?> Get<T>(string key) where T : class
    {
        var value = await _database.StringGetAsync(key);
        if (value.IsNullOrEmpty) return default;

        return JsonConvert.DeserializeObject<T>(value);
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

    public async Task<long> Increase(string key, int increase, TimeSpan? expire)
    {
        var count = await _database.StringIncrementAsync(key, increase);
        if (expire != null)
        {
            _database.KeyExpire(key, expire);
        }

        return count;
    }
}