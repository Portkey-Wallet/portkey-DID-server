using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;

namespace CAServer.CAAccount.Provider;

[RemoteService(false)]
[DisableAuditing]
public class PreValidationProvider : CAServerAppService, IPreValidationProvider
{
    private readonly IEnumerable<IPreValidationStrategy> _preValidationStrategies;
    private readonly IDistributedCache<string> _distributedCache;
    private readonly ILogger<PreValidationProvider> _logger;
    
    public PreValidationProvider(IEnumerable<IPreValidationStrategy> preValidationStrategies,
        IDistributedCache<string> distributedCache,
        ILogger<PreValidationProvider> logger)
    {
        _preValidationStrategies = preValidationStrategies;
        _distributedCache = distributedCache;
        _logger = logger;
    }
    
    public async Task<bool> ValidateSocialRecovery(RequestSource source, string caHash,
        string chainId, string manager, List<GuardianInfo> guardiansApproved, List<ManagerDto> existedManagers)
    {
        // if (!RequestSource.Sdk.Equals(source))
        // {
        //     return true;
        // }
        var sw = new Stopwatch();
        sw.Start();
        //1 manager check todo for test annotate code
        if (!existedManagers.IsNullOrEmpty() && existedManagers.Any(mg => mg.Address.Equals(manager)))
        {
            throw new UserFriendlyException("manager exists error.");
            return false;
        }
        //2 guardian check
        foreach (var guardianInfo in guardiansApproved)
        {
            foreach (var preValidationStrategy in _preValidationStrategies)
            {
                if (preValidationStrategy.ValidateParameters(guardianInfo))
                {
                    var result = await preValidationStrategy.PreValidateGuardian(chainId, caHash, manager, guardianInfo);
                    _logger.LogInformation("============preValidationStrategy succeed");
                    if (!result)
                    {
                        _logger.LogInformation("preValidationStrategy failed type:{0} chainId:{1} caHash:{2} manager:{3} guardianInfo:{4}",
                            preValidationStrategy.Type, chainId, caHash, manager, JsonConvert.SerializeObject(guardianInfo));
                        throw new UserFriendlyException("guardian:" + preValidationStrategy.Type + "pre validation failed");
                        return false;
                    }
                }
            }
        }
        sw.Stop();
        _logger.LogInformation("ValidateSocialRecovery cost:{0}ms", sw.ElapsedMilliseconds);
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
        if (result.IsNullOrEmpty())
        {
            return null;
        }
        return JsonConvert.DeserializeObject<ManagerCacheDto>(result);
    }

    private string GetCacheKey(string manager)
    {
        return "Portkey:SocialRecover:" + manager;
    }
}