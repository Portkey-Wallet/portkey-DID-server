using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Commons.Etos;
using CAServer.Etos.Chain;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.Grain.Chain;
using CAServer.Options;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Environment = CAServer.Options.Environment;
namespace CAServer.Chain;

[RemoteService(false)]
[DisableAuditing]
public class ChainAppService : CAServerAppService, IChainAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly HostInfoOptions _hostInfoOptions;
    
    public ChainAppService(IDistributedEventBus distributedEventBus, IClusterClient clusterClient,
        IObjectMapper objectMapper, IOptionsSnapshot<HostInfoOptions> hostInfoOptions)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _hostInfoOptions = hostInfoOptions.Value;
    }

    public async Task<ChainResultDto> CreateAsync(CreateUpdateChainDto input)
    {
        var chainGrain = _clusterClient.GetGrain<IChainGrain>(input.ChainId);
        var result =
            await chainGrain.AddChainAsync(ObjectMapper.Map<CreateUpdateChainDto, ChainGrainDto>(input));

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(_objectMapper.Map<ChainGrainDto, ChainCreateEto>(result.Data));

        return _objectMapper.Map<ChainGrainDto, ChainResultDto>(result.Data);
    }

    public async Task<ChainResultDto> UpdateAsync(string id, CreateUpdateChainDto input)
    {
        if (id != input.ChainId)
        {
            throw new UserFriendlyException("chainId can not modify.");
        }

        var chainGrain = _clusterClient.GetGrain<IChainGrain>(id);
        var result =
            await chainGrain.UpdateChainAsync(ObjectMapper.Map<CreateUpdateChainDto, ChainGrainDto>(input));

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(_objectMapper.Map<ChainGrainDto, ChainUpdateEto>(result.Data));

        return _objectMapper.Map<ChainGrainDto, ChainResultDto>(result.Data);
    }

    public async Task DeleteAsync(string chainId)
    {
        var chainGrain = _clusterClient.GetGrain<IChainGrain>(chainId);
        var result = await chainGrain.DeleteChainAsync();

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(_objectMapper.Map<ChainGrainDto, ChainDeleteEto>(result.Data));
    }
    public async Task<Dictionary<string, ChainDisplayNameDto>> ListChainDisplayInfos(string chainId)
    {
        var result = new Dictionary<string, ChainDisplayNameDto>();
        if (chainId.IsNullOrEmpty())
        {
            switch (_hostInfoOptions.Environment)
            {
                case Environment.Production:
                {
                    AddChainDisplayInfo(result, CommonConstant.TDVVChainId);
                    break;
                }
                case Environment.Development:
                case Environment.Staging:
                default:
                {
                    AddChainDisplayInfo(result, CommonConstant.TDVWChainId);
                    break;
                }
            }
            AddChainDisplayInfo(result, CommonConstant.MainChainId);
        }
        else
        {
            var displayName = ChainDisplayNameHelper.DisplayNameMap.GetValueOrDefault(chainId);
            if (displayName.IsNullOrEmpty())
            {
                throw new UserFriendlyException("the display name of the chain doesn't exist");
            }

            var iconUrl =  ChainDisplayNameHelper.ChainImageUrlMap.GetValueOrDefault(chainId);
            if (iconUrl.IsNullOrEmpty())
            {
                throw new UserFriendlyException("the icon url of the chain doesn't exist");
            }
            AddChainDisplayInfo(result, chainId);
        }

        return result;
    }

    private static void AddChainDisplayInfo(IDictionary<string, ChainDisplayNameDto> result, string chainId)
    {
        result.Add(chainId, BuildChainDisplayNameDto(chainId));
    }

    private static ChainDisplayNameDto BuildChainDisplayNameDto(string chainId)
    {
        return new ChainDisplayNameDto()
        {
            DisplayChainName = ChainDisplayNameHelper.MustGetChainDisplayName(chainId),
            ChainImageUrl = ChainDisplayNameHelper.MustGetChainUrl(chainId)
        };
    }
}