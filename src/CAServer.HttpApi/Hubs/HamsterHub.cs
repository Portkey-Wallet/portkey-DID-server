using System;
using System.Threading.Tasks;
using CAServer.EnumType;
using CAServer.Growth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.Hubs;

[Authorize]
public class HamsterHub : AbpHub
{
    private readonly IHubService _hubService;
    private readonly ILogger<HamsterHub> _logger;

    public HamsterHub(ILogger<HamsterHub> logger, IHubService hubService)
    {
        _logger = logger;
        _hubService = hubService;
    }


    public async Task Connect(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            return;
        }

        await _hubService.RegisterClient(clientId, Context.ConnectionId);
        _logger.LogInformation("clientId={ClientId} connect", clientId);
        await _hubService.SendAllUnreadRes(clientId);
    }
    
    public async Task RewardProgress(ActivityEnums activityEnums,string targetClientId)
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new UnauthorizedAccessException();
        }

        await _hubService.RewardProgressAsync(activityEnums,targetClientId);
    }
    

    public async Task ReferralRecordList(ReferralRecordRequestDto input)
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new UnauthorizedAccessException();
        }
    
        await _hubService.ReferralRecordListAsync(input);
    }
    
    //
    // public async Task<ReferralRecordsRankResponseDto> GetReferralRecordRank(ReferralRecordRankRequestDto input)
    // {
    //     if (!CurrentUser.Id.HasValue)
    //     {
    //         throw new UnauthorizedAccessException();
    //     }
    //
    //     return await _hubService.GetReferralRecordRankAsync(input);
    // }
    

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var clientId = _hubService.UnRegisterClient(Context.ConnectionId);
        _logger.LogInformation("clientId={ClientId} disconnected!!!", clientId);
        return base.OnDisconnectedAsync(exception);
    }
}