using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Growth.Dtos;
using CAServer.Message.Dtos;
using CAServer.Message.Etos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.Hubs;

[Authorize]
public class HamsterHub : AbpHub
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IHubService _hubService;
    private readonly ILogger<HamsterHub> _logger;

    public HamsterHub(ILogger<HamsterHub> logger, IHubService hubService, IDistributedEventBus distributedEventBus)
    {
        _logger = logger;
        _hubService = hubService;
        _distributedEventBus = distributedEventBus;
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

    public async Task<ReferralRecordResponseDto> ReferralRecordList(ReferralRecordRequestDto input)
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new UnauthorizedAccessException();
        }

        return await _hubService.ReferralRecordListAsync(input);
    }
    
    public async Task<ReferralRecordsRankResponseDto> GetReferralRecordRank(ReferralRecordRankRequestDto input)
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new UnauthorizedAccessException();
        }

        return await _hubService.GetReferralRecordRankAsync(input);
    }
    

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var clientId = _hubService.UnRegisterClient(Context.ConnectionId);
        _logger.LogInformation("clientId={ClientId} disconnected!!!", clientId);
        return base.OnDisconnectedAsync(exception);
    }
}