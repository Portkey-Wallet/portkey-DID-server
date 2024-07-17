using System.Threading.Tasks;
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
        mintInfoDto.TokenId = "1";

        return mintInfoDto;
    }

    public Task<ConfirmDto> ConfirmAsync(ConfirmRequestDto requestDto)
    {
        var collectionInfo = _freeMintOptions.CollectionInfo;
        
        // set grain info , status->pending
        // set status in es
        // send transaction
        // 
        throw new System.NotImplementedException();
    }

    public Task<ConfirmDto> MintAgainAsync(MintAgainRequestDto requestDto)
    {
        throw new System.NotImplementedException();
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

    public Task<GetNftItemDetailDto> GetNftItemDetailAsync(string itemId)
    {
        throw new System.NotImplementedException();
    }
}