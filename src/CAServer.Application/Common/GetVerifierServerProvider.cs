using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Options;
using CAServer.Settings;
using CAServer.Verifier;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUglify.Helpers;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using ChainOptions = CAServer.Options.ChainOptions;

namespace CAServer.Common;

public interface IGetVerifierServerProvider
{
    public Task<string> GetVerifierServerEndPointsAsync(string verifierId, string chainId);
}

public class GetVerifierServerProvider : IGetVerifierServerProvider, ISingletonDependency
{
    private readonly ChainOptions _chainOptions;
    private readonly IDistributedCache<GuardianVerifierServerCacheItem> _distributedCache;
    private readonly AdaptableVariableOptions _adaptableVariableOptions;
    private readonly ILogger<GetVerifierServerProvider> _logger;


    private const string VerifierServerListCacheKey = "CAVerifierServer";

    public GetVerifierServerProvider(IOptions<ChainOptions> chainOptions,
        IDistributedCache<GuardianVerifierServerCacheItem> distributedCache,
        IOptions<AdaptableVariableOptions> adaptableVariableOptions, ILogger<GetVerifierServerProvider> logger)
    {
        _adaptableVariableOptions = adaptableVariableOptions.Value;
        _distributedCache = distributedCache;
        _logger = logger;
        _chainOptions = chainOptions.Value;
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

    private int GetRandomNum(int t)
    {
        var random = new Random();
        return random.Next(0, t);
    }


    private async Task<GetVerifierServersOutput> GetVerifierServersListAsync(string chainId)
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainOption))
        {
            return null;
        }

        var client = new AElfClient(chainOption.BaseUrl);
        await client.IsConnectedAsync();
        var ownAddress = client.GetAddressFromPrivateKey(chainOption.PrivateKey);
        const string methodName = "GetVerifierServers";

        var param = new Empty();
        var transaction = await client.GenerateTransactionAsync(ownAddress,
            chainOption.ContractAddress, methodName, param);
        var txWithSign = client.SignTransaction(chainOption.PrivateKey, transaction);
        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });
        return GetVerifierServersOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result));
    }

    private async Task<GuardianVerifierServerCacheItem> GetVerifierServerListAsync(string chainId)
    {
        var result = await GetVerifierServersListAsync(chainId);
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
                EndPoints = t.EndPoints.ToList()
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
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(_adaptableVariableOptions.VerifierServerExpireTime)
            }
        );
    }
}