using System.Threading.Tasks;
using CAServer.FreeMint.Dtos;
using CAServer.Grains.Grain.FreeMint;
using Microsoft.Extensions.Logging;
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

    public FreeMintAppService(ILogger<FreeMintAppService> logger, IClusterClient clusterClient)
    {
        _logger = logger;
        _clusterClient = clusterClient;
    }

    public async Task<GetRecentStatusDto> GetRecentStatusAsync()
    {
        var grain = _clusterClient.GetGrain<IFreeMintGrain>(CurrentUser.GetId());
        return (await grain.GetRecentStatus()).Data;
    }

    public Task<GetMintInfoDto> GetMintInfoAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task<ConfirmDto> ConfirmAsync(ConfirmRequestDto requestDto)
    {
        throw new System.NotImplementedException();
    }

    public Task<ConfirmDto> MintAgainAsync(MintAgainRequestDto requestDto)
    {
        throw new System.NotImplementedException();
    }

    public Task<GetStatusDto> GetStatusAsync(string itemId)
    {
        throw new System.NotImplementedException();
    }

    public Task<GetNftItemDetailDto> GetNftItemDetailAsync(string itemId)
    {
        throw new System.NotImplementedException();
    }
}