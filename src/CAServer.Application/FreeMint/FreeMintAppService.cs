using System;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.EnumType;
using CAServer.FreeMint.Dtos;
using CAServer.Grains.Grain.FreeMint;
using CAServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace CAServer.FreeMint;

[RemoteService(false), DisableAuditing]
public class FreeMintAppService : CAServerAppService, IFreeMintAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<FreeMintAppService> _logger;
    private readonly FreeMintOptions _freeMintOptions;

    public FreeMintAppService(ILogger<FreeMintAppService> logger, IClusterClient clusterClient,
        IOptionsSnapshot<FreeMintOptions> freeMintOptions)
    {
        _logger = logger;
        _clusterClient = clusterClient;

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

        var grain = _clusterClient.GetGrain<ITokenIdGrain>(CommonConstant.FreeMintTokenIdGrainId);
        mintInfoDto.TokenId = await grain.GenerateTokenId();
        return mintInfoDto;
    }

    public async Task<ConfirmDto> ConfirmAsync(ConfirmRequestDto requestDto)
    {
        var collectionInfo = _freeMintOptions.CollectionInfo;

        // set grain info , status->pending
        // set status in es
        // send transaction
        // 
        return new ConfirmDto()
        {
            ItemId = Guid.NewGuid().ToString()
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

    public async Task<GetItemInfoDto> GetItemInfoAsync(string itemId)
    {
        return new GetItemInfoDto()
        {
            ImageUrl = "https://forest-testnet.s3.ap-northeast-1.amazonaws.com/294xAUTO/1718204222065-Activity%20Icon.png",
            Name = "test",
            TokenId = "10",
            Description = "mock data"
        };
    }
}