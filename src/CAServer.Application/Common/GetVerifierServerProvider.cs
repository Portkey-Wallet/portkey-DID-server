using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Settings;
using CAServer.Verifier;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NUglify.Helpers;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace CAServer.Common;

public interface IGetVerifierServerProvider
{
    public Task<VerifierServerInfo> GetVerifierServerAsync(string verifierId, string chainId);
    public Task<string> GetVerifierServerEndPointsAsync(string verifierId, string chainId);
    
    public Task<string> GetFirstVerifierServerEndPointAsync(string chainId);

    public Task<VerifierServersBasicInfoResponse> GetVerifierServerDetailsAsync(string chainId);

    public Task RemoveVerifierServerDetailsCacheAsync(string chainId);
}

public class GetVerifierServerProvider : IGetVerifierServerProvider, ISingletonDependency
{
    private readonly IDistributedCache<GuardianVerifierServerCacheItem> _distributedCache;
    private readonly AdaptableVariableOptions _adaptableVariableOptions;
    private readonly ILogger<GetVerifierServerProvider> _logger;
    private readonly IContractProvider _contractProvider;
    private readonly IDistributedCache<VerifierServersBasicInfoResponse> _verifierServerCache;


    private const string VerifierServerListCacheKey = "CAVerifierServer";
    private const string VerifierServerListWithDetailCacheKey = "CAVerifierServerWithDetail";

    public GetVerifierServerProvider(
        IDistributedCache<GuardianVerifierServerCacheItem> distributedCache,
        IOptionsSnapshot<AdaptableVariableOptions> adaptableVariableOptions, ILogger<GetVerifierServerProvider> logger,
        IContractProvider contractProvider,
        IDistributedCache<VerifierServersBasicInfoResponse> verifierServerCache)
    {
        _adaptableVariableOptions = adaptableVariableOptions.Value;
        _distributedCache = distributedCache;
        _logger = logger;
        _contractProvider = contractProvider;
        _verifierServerCache = verifierServerCache;
    }
    
    public async Task<VerifierServerInfo> GetVerifierServerAsync(string verifierId, string chainId)
    {
        //GetVerifiereServerList
        var verifierServerDto = await GetVerifierServerAsync(chainId);
        if (verifierServerDto == null || verifierServerDto.GuardianVerifierServers.Count == 0)
        {
            _logger.LogInformation($"No Available Service Tips,Invalid VerifierId is : {verifierId}");
            return null;
        }

        var servers = verifierServerDto.GuardianVerifierServers;
        return servers.FirstOrDefault(p => p.Id.ToString() == verifierId);
    }


    public async Task<string> GetVerifierServerEndPointsAsync(string verifierId, string chainId)
    {
        //GetVerifiereServerList
        var verifierServerDto = await GetVerifierServerAsync(chainId);
        if (verifierServerDto == null || verifierServerDto.GuardianVerifierServers.Count == 0)
        {
            _logger.LogInformation($"No Available Service Tips,Invalid VerifierId is : {verifierId}");
            return null;
        }

        var servers = verifierServerDto.GuardianVerifierServers;
        var verifierServerInfo = servers.FirstOrDefault(p => p.Id.ToString() == verifierId);
        if (verifierServerInfo != null)
        {
            return verifierServerInfo.EndPoints[GetRandomNum(verifierServerInfo.EndPoints.Count)];
        }

        _logger.LogInformation(
            $"Http request sender is not in verifier server list,Invalid VerifierId is : {verifierId}");
        return null;
    }

    public async Task<string> GetFirstVerifierServerEndPointAsync(string chainId)
    {
        var verifierServerDto = await GetVerifierServerAsync(chainId);
        if (verifierServerDto == null || verifierServerDto.GuardianVerifierServers.Count == 0)
        {
            _logger.LogInformation($"No Available Service Tips,Invalid chainId is : {chainId}");
            return null;
        }

        var servers = verifierServerDto.GuardianVerifierServers;
        return servers[0].EndPoints[0];
    }

    private int GetRandomNum(int t)
    {
        var random = new Random();
        return random.Next(0, t);
    }

    private async Task<GuardianVerifierServerCacheItem> GetVerifierServerListAsync(string chainId)
    {
        var result = await _contractProvider.GetVerifierServersListAsync(chainId);

        if (null == result)
        {
            return null;
        }

        var verifierServerList = BuildVerifierServerList(result);
        return new GuardianVerifierServerCacheItem
        {
            GuardianVerifierServers = verifierServerList
        };
    }

    private List<VerifierServerInfo> BuildVerifierServerList(GetVerifierServersOutput output)
    {
        var verifierServerList = new List<VerifierServerInfo>();
        var servers = output.VerifierServers;
        servers.ForEach(t =>
        {
            verifierServerList.Add(new VerifierServerInfo
            {
                Id = t.Id.ToHex(),
                EndPoints = t.EndPoints.ToList(),
                VerifierAddresses = t.VerifierAddresses.Select(ad => ad.ToBase58()).ToList()
            });
        });
        return verifierServerList;
    }

    private async Task<GuardianVerifierServerCacheItem> GetVerifierServerAsync(string chainId)
    {
        return await _distributedCache.GetOrAddAsync(
            string.Join(":", VerifierServerListCacheKey, chainId),
            async () => await GetVerifierServerListAsync(chainId),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddDays(_adaptableVariableOptions.VerifierServerExpireTime)
            }
        );
    }
    
    public async Task<VerifierServersBasicInfoResponse> GetVerifierServerDetailsAsync(string chainId)
    {
        return await _verifierServerCache.GetOrAddAsync(
            string.Join(":", VerifierServerListWithDetailCacheKey, chainId),
            async () => await GetVerifierServersAsync(chainId),
            () => new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_adaptableVariableOptions.VerifierServerExpireTime)
            }
        );
    }
    
    public async Task RemoveVerifierServerDetailsCacheAsync(string chainId)
    {
        await _verifierServerCache.RemoveAsync(
            string.Join(":", VerifierServerListWithDetailCacheKey, chainId)
        );
    }
    
    private async Task<VerifierServersBasicInfoResponse> GetVerifierServersAsync(string chainId)
    {
        var result = await _contractProvider.GetVerifierServersListAsync(chainId);

        if (null == result)
        {
            return null;
        }

        var verifierServerList = BuildVerifierServers(result);
        return new VerifierServersBasicInfoResponse
        {
            GuardianVerifierServers = verifierServerList
        };
    }

    private List<VerifierServerBasicInfo> BuildVerifierServers(GetVerifierServersOutput output)
    {
        var verifierServers = new List<VerifierServerBasicInfo>();
        var servers = output.VerifierServers;
        servers.ForEach(t =>
        {
            verifierServers.Add(new VerifierServerBasicInfo
            {
                Id = t.Id.ToHex(),
                Name = t.Name,
                ImageUrl = t.ImageUrl,
                EndPoints = t.EndPoints.ToList(),
                VerifierAddresses = t.VerifierAddresses.Select(address => address.ToBase58()).ToList()
            });
        });
        return verifierServers;
    }
}