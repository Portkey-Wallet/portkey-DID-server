using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Volo.Abp.Caching;

namespace CAServer.CAAccount.Provider;

public class PreValidationProvider : IPreValidationProvider
{
    private readonly IEnumerable<IPreValidationStrategy> _preValidationStrategies;
    private readonly DistributedCache<string> _distributedCache;
    
    public PreValidationProvider(IEnumerable<IPreValidationStrategy> preValidationStrategies,
        DistributedCache<string> distributedCache)
    {
        _preValidationStrategies = preValidationStrategies;
        _distributedCache = distributedCache;
    }
    
    public async Task<bool> ValidateSocialRecovery(RequestSource source, string caHash,
        string chainId, string manager, List<GuardianInfo> guardiansApproved)
    {
        if (!RequestSource.Sdk.Equals(source))
        {
            return true;
        }

        foreach (var guardianInfo in guardiansApproved)
        {
            foreach (var preValidationStrategy in _preValidationStrategies)
            {
                if (preValidationStrategy.ValidateParameters(guardianInfo))
                {
                    var result = await preValidationStrategy.PreValidateGuardian(chainId, caHash, manager, guardianInfo);
                    if (!result)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    public async Task SaveManagerInCache(string manager, string caHash, string caAddress)
    {
        var managerCacheDto = new ManagerCacheDto()
        {
            CaHash = caHash,
            CaAddress = caAddress
        };
        await _distributedCache.SetAsync(GetCacheKey(manager), JsonConvert.SerializeObject(managerCacheDto), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) });
    }

    public async Task<ManagerCacheDto> GetManagerFromCache(string manager)
    {
        var result = await _distributedCache.GetAsync(GetCacheKey(manager));
        return JsonConvert.DeserializeObject<ManagerCacheDto>(result);
    }

    private string GetCacheKey(string manager)
    {
        return "Portkey:SocialRecover:" + manager;
    }
}