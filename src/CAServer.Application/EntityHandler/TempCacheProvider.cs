using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Cache;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace CAServer.EntityHandler;

public interface ITempCacheProvider
{
    public Task<Dictionary<string, string>> GetCacheByModuleAsync(string module);
    public Task<bool> SetCacheAsync(string module, string key, string value);
    public Task<bool> RemoveCacheAsync(string module, string key, string value);
    public Task<bool> RemoveCacheByFullKeyAsync(string module, string fullKey, string value);
}

public class TempCacheProvider : ITempCacheProvider, ISingletonDependency
{
    private readonly ILogger<TempCacheProvider> _logger;
    private readonly ICacheProvider _cacheProvider;


    public TempCacheProvider(
        ILogger<TempCacheProvider> logger, ICacheProvider cacheProvider)
    {
        _logger = logger;
        _cacheProvider = cacheProvider;
    }


    private int loginRegisterTimeoutSeconds = 60;
    private int loginRegisterNumber = 10;


    public async Task<Dictionary<string, string>> GetCacheByModuleAsync(string module)
    {
        _logger.LogDebug("TempCacheProvider GetCacheByModuleAsync module = {0} ", module);
        Dictionary<string, string> convertedDictionary = new Dictionary<string, string>();

        try
        {
            var keys = await _cacheProvider.SetMembersAsync(GetCacheKey(module));
            if (keys == null || keys.Length == 0)
            {
                return convertedDictionary;
            }

            List<string> keysList = keys.Select(redisValue => redisValue.ToString()).ToList();
            var values = await _cacheProvider.BatchGet(keysList);
            foreach (var kvp in values)
            {
                convertedDictionary[GetKey(kvp.Key)] = kvp.Value.ToString();
            }

            return convertedDictionary;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TempCacheProvider GetCacheByModuleAsync module = {0} has error", module);
        }

        return convertedDictionary;
    }

    public async Task<bool> SetCacheAsync(string module, string key, string value)
    {
        _logger.LogDebug("TempCacheProvider SetCacheAsync module = {0} key = {1} value = {2} has error", module, key, value);
        try
        {
            string fullKey = GetCacheKey(module, key);
            await _cacheProvider.SetAddAsync(GetCacheKey(module), new List<string> { fullKey }, expire);
            await _cacheProvider.Set(fullKey, value, expire);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TempCacheProvider SetCacheAsync module = {0} key = {1} value = {2} has error", module, key, value);
        }

        return false;
    }

    public async Task<bool> RemoveCacheAsync(string module, string key, string value)
    {
        _logger.LogDebug("TempCacheProvider RemoveCacheAsync module = {0} key = {1} value = {2}", module, key, value);
        try
        {
            string fullKey = GetCacheKey(module, key);
            return await RemoveCacheByFullKeyAsync(module, fullKey, value);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TempCacheProvider RemoveCacheAsync module = {0} key = {1} value = {2} has error", module, key, value);
        }

        return false;
    }

    public async Task<bool> RemoveCacheByFullKeyAsync(string module, string fullKey, string value)
    {
        _logger.LogDebug("TempCacheProvider RemoveCacheByFullKeyAsync module = {0} key = {1} value = {2}", module, fullKey, value);
        try
        {
            await _cacheProvider.SetRemoveAsync(GetCacheKey(module), new List<string> { fullKey });
            await _cacheProvider.Delete(fullKey);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TempCacheProvider RemoveCacheByFullKeyAsync module = {0} key = {1} value = {2} has error", module, fullKey, value);
        }

        return false;
    }

    private TimeSpan expire = TimeSpan.FromDays(2);
    private string modulePrefix = "EntityHandler";

    private string GetCacheKey(params string[] keys)
    {
        return string.Join(":", modulePrefix, string.Join(":", keys));
    }

    private string GetKey(string fullKey)
    {
        return fullKey.Split(":")[2];
    }
}