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

    public async Task AddScoreAsync(string leaderboardKey, string member, double score)
    {
        await _database.SortedSetAddAsync(leaderboardKey, member, score);
    }

    public async Task<double> GetScoreAsync(string leaderboardKey, string member)
    {
        return await _database.SortedSetScoreAsync(leaderboardKey, member) ?? 0;
    }

    public async Task<long> GetRankAsync(string leaderboardKey, string member, bool highToLow = true)
    {
        long? rank;

        if (highToLow)
        {
            rank = await _database.SortedSetRankAsync(leaderboardKey, member, Order.Descending);
        }
        else
        {
            rank = await _database.SortedSetRankAsync(leaderboardKey, member);
        }

        return rank ?? -1; // -1 indicates that the member is not in the leaderboard
    }

    public async Task<SortedSetEntry[]> GetTopAsync(string leaderboardKey, long startRank, long stopRank, bool highToLow = true)
    {
        var order = highToLow ? Order.Descending : Order.Ascending;
        return await _database.SortedSetRangeByRankWithScoresAsync(leaderboardKey, startRank, stopRank, order);
    }

    public async Task<long> GetSortedSetLengthAsync(string leaderboardKey)
    {
        var length = await _database.SortedSetLengthAsync(leaderboardKey);
        return length;
    }

    public async Task SetAddAsync(string key, string value, TimeSpan? expire)
    {
        await _database.SetAddAsync(key, value);
        if (expire != null)
        {
            _database.KeyExpire(key, expire);
        }
    }

    public async Task SetAddAsync(string key, List<string> values, TimeSpan? expire)
    {
        RedisValue[] vList = values.Select(p => new RedisValue(p)).ToArray();
        await _database.SetAddAsync(key, vList);
        if (expire != null)
        {
            _database.KeyExpire(key, expire);
        }
    }

    public async Task SetRemoveAsync(string key, List<string> values)
    {
        RedisValue[] vList = values.Select(p => new RedisValue(p)).ToArray();
        await _database.SetRemoveAsync(key, vList);
    }

    public async Task<RedisValue[]> SetMembersAsync(string key)
    {
        return await _database.SetMembersAsync(key);
    }
}