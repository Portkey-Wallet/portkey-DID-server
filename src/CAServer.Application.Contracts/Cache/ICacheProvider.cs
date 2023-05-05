using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace CAServer.Cache;

public interface ICacheProvider
{
    Task HSetWithExpire(string key, string member, string value, TimeSpan? expiry);
    Task<bool> HashDelete(string key, string member);
    Task<HashEntry[]> HGetAll(string key);
    Task Set(string key, string value, TimeSpan? expiry);
    Task<RedisValue> Get(string key);
    Task Delete(string key);
    Task<Dictionary<string, RedisValue>> BatchGet(List<string> keys);
    Task<long> Increase(string key, int increase,TimeSpan? expiry);
}