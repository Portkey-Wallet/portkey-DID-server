using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace CAServer.Cache;

public interface ICacheProvider
{
    Task HSetWithExpire(string key, string member, string value, TimeSpan? expire);
    Task<bool> HashDeleteAsync(string key, string member);
    Task<HashEntry[]> HGetAll(string key);
    Task Set(string key, string value, TimeSpan? expire);
    Task Set<T>(string key, T value, TimeSpan? expire) where T : class;
    Task<RedisValue> Get(string key);
    Task<T> Get<T>(string key) where T : class;
    Task Delete(string key);
    Task<Dictionary<string, RedisValue>> BatchGet(List<string> keys);
    Task<long> Increase(string key, int increase,TimeSpan? expire);

    Task AddScoreAsync(string leaderboardKey, string member, double score);

    Task<double> GetScoreAsync(string leaderboardKey, string member);

    Task<long> GetRankAsync(string leaderboardKey, string member, bool highToLow = true);

    Task<SortedSetEntry[]> GetTopAsync(string leaderboardKey, long startRank, long stopRank, bool highToLow = true);

    Task<long> GetSortedSetLengthAsync(string leaderboardKey);

    Task SetAddAsync(string key, string value, TimeSpan? timeSpan);
    
    Task SetAddAsync(string key, List<string> values, TimeSpan? timeSpan);
    
    Task SetRemoveAsync(string key, List<string> values);

    Task<RedisValue[]> SetMembersAsync(string key);





}