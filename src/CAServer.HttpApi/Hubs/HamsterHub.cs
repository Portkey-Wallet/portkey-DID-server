using System;
using System.Threading.Tasks;
using CAServer.Growth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.AspNetCore.SignalR;

namespace CAServer.Hubs;

[HubRoute("HamsterDataReporting")]
[Authorize]
public class HamsterHub : AbpHub
{
    // private readonly IHubService _hubService;
    // private readonly ILogger<HamsterHub> _logger;
    //
    // public HamsterHub(ILogger<HamsterHub> logger, IHubService hubService)
    // {
    //     _logger = logger;
    //     _hubService = hubService;
    // }
    //
    // public override Task OnConnectedAsync()
    // {
    //     _logger.LogInformation("user connected");
    //     return base.OnConnectedAsync();
    // }
    //
    //
    // public async Task Connect(string clientId)
    // {
    //     if (string.IsNullOrEmpty(clientId))
    //     {
    //         return;
    //     }
    //
    //     await _hubService.RegisterClient(clientId, Context.ConnectionId);
    //     _logger.LogInformation("clientId={ClientId} connect", clientId);
    //     await _hubService.SendAllUnreadRes(clientId);
    // }
    //
    // public async Task RewardProgress(RewardProgressDto rewardProgressDto)
    // {
    //     if (!CurrentUser.Id.HasValue)
    //     {
    //         throw new UnauthorizedAccessException();
    //     }
    //
    //     //await _hubService.RewardProgressAsync(rewardProgressDto.ActivityEnums,rewardProgressDto.TargetClientId);
    // }
    //
    //
    // public async Task ReferralRecordList(ReferralRecordRequestDto input)
    // {
    //     _logger.LogDebug("Hub param is {param}",JsonConvert.SerializeObject(input));
    //     if (!CurrentUser.Id.HasValue)
    //     {
    //         throw new UnauthorizedAccessException();
    //     }
    //
    //     //await _hubService.ReferralRecordListAsync(input);
    // }
    //
    //
    //
    // public override Task OnDisconnectedAsync(Exception? exception)
    // {
    //     var clientId = _hubService.UnRegisterClient(Context.ConnectionId);
    //     _logger.LogInformation("clientId={ClientId} disconnected!!!", clientId);
    //     return base.OnDisconnectedAsync(exception);
    // }
}