using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.FreeMint.Dtos;
using CAServer.FreeMint.Etos;
using CAServer.FreeMint.Provider;
using CAServer.Grains.Grain.FreeMint;
using CAServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace CAServer.FreeMint;

[RemoteService(false), DisableAuditing]
public class FreeMintAppService : CAServerAppService, IFreeMintAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<FreeMintAppService> _logger;
    private readonly FreeMintOptions _freeMintOptions;
    private readonly IFreeMintProvider _freeMintProvider;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ChainOptions _chainOptions;

    public FreeMintAppService(ILogger<FreeMintAppService> logger, IClusterClient clusterClient,
        IOptionsSnapshot<FreeMintOptions> freeMintOptions, IFreeMintProvider freeMintProvider,
        IDistributedEventBus distributedEventBus, IOptionsSnapshot<ChainOptions> chainOptions)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _freeMintProvider = freeMintProvider;
        _distributedEventBus = distributedEventBus;
        _freeMintOptions = freeMintOptions.Value;
        _chainOptions = chainOptions.Value;
    }

    public async Task<GetRecentStatusDto> GetRecentStatusAsync()
    {
        var grain = _clusterClient.GetGrain<IFreeMintGrain>(CurrentUser.GetId());
        return (await grain.GetRecentStatus()).Data;
    }

    public async Task<GetMintInfoDto> GetMintInfoAsync()
    {
        var grain = _clusterClient.GetGrain<IFreeMintGrain>(CurrentUser.GetId());
        var mintInfoDto = new GetMintInfoDto()
        {
            CollectionInfo = _freeMintOptions.CollectionInfo,
            IsLimitExceed = grain.CheckLimitExceed().Result,
            LimitCount = _freeMintOptions.LimitCount,
            ChainId = GetSideChainId()
        };
        return mintInfoDto;
    }

    public async Task<ConfirmDto> ConfirmAsync(ConfirmRequestDto requestDto)
    {
        var collectionInfo = _freeMintOptions.CollectionInfo;
        var grain = _clusterClient.GetGrain<IFreeMintGrain>(CurrentUser.GetId());

        var confirmInfo = ObjectMapper.Map<ConfirmRequestDto, ConfirmGrainDto>(requestDto);
        var saveResult = await grain.SaveMintInfo(new MintNftDto()
        {
            CollectionInfo = collectionInfo,
            ConfirmInfo = confirmInfo
        });

        if (!saveResult.Success)
        {
            throw new UserFriendlyException(saveResult.Message);
        }

        var mintNftInfo = saveResult.Data;
        var eto = new FreeMintEto
        {
            UserId = CurrentUser.GetId(),
            CollectionInfo = collectionInfo,
            ConfirmInfo = mintNftInfo
        };

        await _distributedEventBus.PublishAsync(eto, false, false);
        _logger.LogInformation("publish free mint eto, userId:{userId}, itemId:{itemId}, tokenId:{tokenId}", eto.UserId,
            eto.ConfirmInfo.ItemId, eto.ConfirmInfo.TokenId);

        return new ConfirmDto
        {
            ItemId = mintNftInfo.ItemId,
            Name = mintNftInfo.Name,
            TokenId = mintNftInfo.TokenId,
            Symbol = $"{_freeMintOptions.CollectionInfo.CollectionName.ToUpper()}-{mintNftInfo.TokenId}"
        };
    }

    public async Task<GetStatusDto> GetStatusAsync(string itemId)
    {
        var grain = _clusterClient.GetGrain<IFreeMintGrain>(CurrentUser.GetId());
        var statusResult = await grain.GetMintStatus(itemId);
        if (!statusResult.Success)
        {
            throw new UserFriendlyException(statusResult.Message);
        }

        return new GetStatusDto
        {
            Status = statusResult.Data.Status
        };
    }

    public async Task<GetItemInfoDto> GetItemInfoAsync([Required] string itemId)
    {
        var index = await _freeMintProvider.GetFreeMintItemAsync(itemId);
        return ObjectMapper.Map<FreeMintIndex, GetItemInfoDto>(index);
    }

    private string GetSideChainId()
    {
        var chainIds = _chainOptions.ChainInfos.Keys;
        return chainIds.FirstOrDefault(chainId => chainId != CommonConstant.MainChainId);
    }
}