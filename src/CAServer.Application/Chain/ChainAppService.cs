using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CAServer.Etos.Chain;
using CAServer.Grains.Grain.Account;
using CAServer.Grains.Grain.Chain;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.Chain;

[RemoteService(false)]
[DisableAuditing]
public class ChainAppService : CAServerAppService, IChainAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;

    public ChainAppService(IDistributedEventBus distributedEventBus, IClusterClient clusterClient,
        IObjectMapper objectMapper)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
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
}