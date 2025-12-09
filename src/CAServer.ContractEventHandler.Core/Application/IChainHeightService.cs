using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Options;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IChainHeightService
{
    Task SetChainHeightAsync();
}

public class ChainHeightService : IChainHeightService, ISingletonDependency
{
    private readonly ILogger<ChainHeightService> _logger;
    private readonly IDistributedCache<ChainHeightCache> _distributedCache;
    private readonly ChainOptions _chainOptions;

    public ChainHeightService(ILogger<ChainHeightService> logger, IDistributedCache<ChainHeightCache> distributedCache,
        IOptions<ChainOptions> chainOptions)
    {
        _logger = logger;
        _distributedCache = distributedCache;
        _chainOptions = chainOptions.Value;
    }

    public async Task SetChainHeightAsync()
    {
        _logger.LogInformation("[ChainHeight] Begin get chain height.");
        var sideChainId = _chainOptions.ChainInfos.Keys.First(t => t != CommonConstant.MainChainId);
        var chainHeight = new ChainHeightCache
        {
            MainChainBlockHeight = await GetBlockHeightAsync(CommonConstant.MainChainId),
            SideChainIndexHeight =
                await GetIndexHeightFromMainChainAsync(ChainHelper.ConvertBase58ToChainId(sideChainId)),
            ParentChainHeight = await GetParentChainHeightAsync(sideChainId)
        };

        _logger.LogInformation("[ChainHeight] set chain height success, {data}",
            JsonConvert.SerializeObject(chainHeight));

        await _distributedCache.SetAsync(nameof(ChainHeightCache), chainHeight);
    }

    private async Task<long> GetBlockHeightAsync(string chainId)
    {
        var client = new AElfClient(_chainOptions.ChainInfos[chainId].BaseUrl);
        await client.IsConnectedAsync();
        return await client.GetBlockHeightAsync();
    }

    private async Task<long> GetIndexHeightFromMainChainAsync(int sideChainId)
    {
        var param = new Int32Value
        {
            Value = sideChainId
        };
        var value = await CallTransactionAsync<Int64Value>(MethodName.GetSideChainHeight, param,
            _chainOptions.ChainInfos.GetOrDefault(CommonConstant.MainChainId).CrossChainContractAddress,
            CommonConstant.MainChainId);

        return value.Value;
    }

    private async Task<long> GetParentChainHeightAsync(string sideChainId)
    {
        var result =
            await CallTransactionAsync<Int64Value>(MethodName.GetParentChainHeight, new Empty(),
                _chainOptions.ChainInfos.GetOrDefault(sideChainId).CrossChainContractAddress, sideChainId);

        return result.Value;
    }

    private async Task<T> CallTransactionAsync<T>(string methodName, IMessage param, string contractAddress,
        string chainId) where T : class, IMessage<T>, new()
    {
        if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo))
        {
            return null;
        }

        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();

        var addressFromPrivateKey = client.GetAddressFromPrivateKey(SignatureKeyHelp.CommonPrivateKeyForCallTx);
        var transaction =
            await client.GenerateTransactionAsync(addressFromPrivateKey, contractAddress, methodName, param);

        var txWithSign = client.SignTransaction(SignatureKeyHelp.CommonPrivateKeyForCallTx, transaction);
        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });

        var value = new T();
        value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));

        return value;
    }
}