using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.EnumType;
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

    public FreeMintAppService(ILogger<FreeMintAppService> logger, IClusterClient clusterClient,
        IOptionsSnapshot<FreeMintOptions> freeMintOptions, IFreeMintProvider freeMintProvider,
        IDistributedEventBus distributedEventBus)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _freeMintProvider = freeMintProvider;
        _distributedEventBus = distributedEventBus;
        _freeMintOptions = freeMintOptions.Value;
    }

    public async Task<GetRecentStatusDto> GetRecentStatusAsync()
    {
        var grain = _clusterClient.GetGrain<IFreeMintGrain>(CurrentUser.GetId());
        return (await grain.GetRecentStatus()).Data;
    }

    public async Task<GetMintInfoDto> GetMintInfoAsync()
    {
        var mintInfoDto = new GetMintInfoDto()
        {
            CollectionInfo = _freeMintOptions.CollectionInfo
        };

        var grain = _clusterClient.GetGrain<IFreeMintGrain>(CurrentUser.GetId());
        var resultDto = await grain.GetTokenId();
        mintInfoDto.TokenId = resultDto.Data;
        return mintInfoDto;
    }

    public async Task<ConfirmDto> ConfirmAsync(ConfirmRequestDto requestDto)
    {
        var collectionInfo = _freeMintOptions.CollectionInfo;
        var grain = _clusterClient.GetGrain<IFreeMintGrain>(CurrentUser.GetId());

        // set grain info , status->pending
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
        
        var eto = new FreeMintEto
        {
            UserId = CurrentUser.GetId(),
            CollectionInfo = collectionInfo,
            ConfirmInfo = saveResult.Data
        };
        
        await _distributedEventBus.PublishAsync(eto);
        return new ConfirmDto()
        {
            ItemId = saveResult.Data.ItemId
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
}