using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CAServer.Cache;
using StackExchange.Redis;

namespace CAServer.Hub;

public class MockCacheProvider : ICacheProvider
{
    private readonly ConcurrentDictionary<string, string> _localCache = new() { };
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _localHashMapCache = new() { };

    public MockCacheProvider()
    {
    }

    public Task HSetWithExpire(string key, string member, string value, TimeSpan? expire)
    {
        if (!_localHashMapCache.TryGetValue(key, out var m))
        {
            m = new Dictionary<string, string> { { member, value } };
            _localHashMapCache[key] = m;
            return Task.CompletedTask;
        }

        m[member] = value;
        return Task.CompletedTask;
    }

    public Task<bool> HashDeleteAsync(string key, string member)
    {
        if (_localHashMapCache.TryGetValue(key, out var m))
        {
            m.Remove(member);
            _localHashMapCache[key] = m;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<HashEntry[]> HGetAll(string key)
    {
        _localHashMapCache.TryGetValue(key, out var val);
        var ans = new HashEntry[] { };
        if (val == null) return Task.FromResult(ans);
        ans = new HashEntry[val.Count];
        var i = 0;
        foreach (var kv in val)
        {
            ans[i] = new HashEntry(new RedisValue(kv.Key), new RedisValue(kv.Value));
            i++;
        }

        return Task.FromResult(ans);
    }

    public Task Set(string key, string value, TimeSpan? expire)
    {
        _localCache[key] = value;
        return Task.CompletedTask;
    }

    public Task Set<T>(string key, T value, TimeSpan? expire) where T : class
    {
        throw new NotImplementedException();
    }

    public Task<RedisValue> Get(string key)
    {
        _localCache.TryGetValue(key, out var val);
        return Task.FromResult(new RedisValue(val));
    }

    public Task<T> Get<T>(string key) where T : class
    {
        throw new NotImplementedException();
    }

    public Task Delete(string key)
    {
        _localCache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<Dictionary<string, RedisValue>> BatchGet(List<string> keys)
    {
        var ans = new Dictionary<string, RedisValue>();
        foreach (var key in keys)
        {
            _localCache.TryGetValue(key, out var val);
            if (val != null) ans[key] = new RedisValue(val);
        }

        return Task.FromResult(ans);
    }

    public Task<long> Increase(string key, int increase, TimeSpan? expire)
    {
        _localCache.TryGetValue(key,out var val);
        if (val.IsNullOrEmpty())
        {
            return Task.FromResult<long>(0);
        }
        return !val.IsNullOrEmpty() ? Task.FromResult(long.Parse(val)) : null;
    }

    public Task AddScoreAsync(string leaderboardKey, string member, double score)
    {
        throw new NotImplementedException();
    }

    public Task<double> GetScoreAsync(string leaderboardKey, string member)
    {
        throw new NotImplementedException();
    }

    public Task<long> GetRankAsync(string leaderboardKey, string member, bool highToLow = true)
    {
        throw new NotImplementedException();
    }

    public Task<SortedSetEntry[]> GetTopAsync(string leaderboardKey, long startRank, long stopRank, bool highToLow = true)
    {
        throw new NotImplementedException();
    }

    public Task<long> GetSortedSetLengthAsync(string leaderboardKey)
    {
        throw new NotImplementedException();
    }

    public Task SetAddAsync(string key, string value, TimeSpan? timeSpan)
    {
        throw new NotImplementedException();
    }

    public Task SetAddAsync(string key, List<string> values, TimeSpan? timeSpan)
    {
        throw new NotImplementedException();
    }

    public Task SetRemoveAsync(string key, List<string> values)
    {
        throw new NotImplementedException();
    }

    public Task<RedisValue[]> SetMembersAsync(string key)
    {
        throw new NotImplementedException();
    }
}