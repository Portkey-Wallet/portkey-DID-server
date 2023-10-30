using System;
using System.Threading.Tasks;
using CAServer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.Hubs;

public class DataReportingHub : AbpHub
{
    private readonly ILogger<DataReportingHub> _logger;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IHubService _hubService;


    public DataReportingHub(ILogger<DataReportingHub> logger, IDistributedEventBus distributedEventBus,
        IHubService hubService)
    {
        _logger = logger;
        _distributedEventBus = distributedEventBus;
        _hubService = hubService;
    }

    public override Task OnConnectedAsync()
    {
        // online
        // string token = _httpContextAccessor.HttpContext?.Request.Query["access_token"];
        //
        // if (token.IsNullOrWhiteSpace())
        // {
        //     return null;
        // }

        return base.OnConnectedAsync();
    }

    public async Task Connect(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            return;
        }

        await _hubService.RegisterClient(clientId, Context.ConnectionId);
        _logger.LogInformation("clientId={ClientId} connect", clientId);
    }

    public Task ReportDeviceInfo(Reporting input)
    {
        _logger.LogInformation("report DeviceInfo, {data}", JsonConvert.SerializeObject(input));
        return Task.CompletedTask;
    }

    public Task ReportAppStatus(ReportingData input)
    {
        _logger.LogInformation("report status, {data}", JsonConvert.SerializeObject(input));
        return Task.CompletedTask;
    }
    
    public Task Logout(ReportingData input)
    {
        _logger.LogInformation("logout, {data}", JsonConvert.SerializeObject(input));
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        // offline
        _logger.LogInformation("disconnect, {data}", nameof(OnDisconnectedAsync));
        return Task.CompletedTask;
    }
}