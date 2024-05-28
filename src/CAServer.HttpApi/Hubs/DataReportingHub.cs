using System;
using System.Threading.Tasks;
using CAServer.DataReporting;
using CAServer.DataReporting.Dtos;
using CAServer.Hub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace CAServer.Hubs;

[HubRoute("dataReporting")]
[Authorize]
public class DataReportingHub : AbpHub
{
    private readonly ILogger<DataReportingHub> _logger;
    private readonly IHubService _hubService;
    private readonly IDeviceInfoReportAppService _deviceInfoReportAppService;
    private readonly IObjectMapper _objectMapper;
    private readonly IConnectionProvider _connectionProvider;
    private readonly MessagePushOptions _messagePushOptions;

    public DataReportingHub(ILogger<DataReportingHub> logger,
        IHubService hubService,
        IDeviceInfoReportAppService deviceInfoReportAppService,
        IObjectMapper objectMapper,
        IConnectionProvider connectionProvider,
        IOptionsSnapshot<MessagePushOptions> messagePushOptions)
    {
        _logger = logger;
        _hubService = hubService;
        _deviceInfoReportAppService = deviceInfoReportAppService;
        _objectMapper = objectMapper;
        _connectionProvider = connectionProvider;
        _messagePushOptions = messagePushOptions.Value;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("user connected");
        return base.OnConnectedAsync();
    }

    public async Task Connect(string clientId)
    {
        if (!CheckIsOpen())
        {
            return;
        }
        
        if (string.IsNullOrEmpty(clientId))
        {
            _logger.LogWarning("connect, clientId is empty, userId:{userId}", CurrentUser.GetId());
            return;
        }

        var id = CurrentUser.Id;
        await _hubService.RegisterClient(clientId, Context.ConnectionId);
        _logger.LogInformation("clientId={ClientId} connect, userId:{userId}", clientId, id);
    }

    public async Task ReportDeviceInfo(UserDeviceReportingRequestDto input)
    {
        var inputJson = System.Text.Json.JsonSerializer.Serialize(input);
        _logger.LogInformation($"ReportDeviceInfo input: {inputJson}");
        
        if (!CheckIsOpen())
        {
            return;
        }
        
        var deviceDto = _objectMapper.Map<UserDeviceReportingRequestDto, UserDeviceReportingDto>(input);
        deviceDto.UserId = CurrentUser.GetId();

        await _deviceInfoReportAppService.ReportDeviceInfoAsync(deviceDto);
        _logger.LogInformation("report deviceInfo, userId:{userId}, deviceId:{deviceId}", deviceDto.UserId,
            input.DeviceId);
    }

    public async Task ReportAppStatus(AppStatusReportingRequestDto input)
    {
        
        var inputJson = System.Text.Json.JsonSerializer.Serialize(input);
        _logger.LogInformation($"ReportAppStatus input: {inputJson}");
        
        if (!CheckIsOpen())
        {
            return;
        }
        
        var deviceId = _connectionProvider.GetConnectionByConnectionId(Context.ConnectionId)?.ClientId;
        if (!CheckDeviceId(deviceId))
        {
            return;
        }
        var appStatusDto = _objectMapper.Map<AppStatusReportingRequestDto, AppStatusReportingDto>(input);
        appStatusDto.UserId = CurrentUser.GetId();
        appStatusDto.DeviceId = deviceId;

        await _deviceInfoReportAppService.ReportAppStatusAsync(appStatusDto);
        _logger.LogDebug(
            "report status, userId: {userId}, deviceId:{deviceId}, appStatus:{appStatus}, unreadCount:{unreadCount}",
            appStatusDto.UserId, appStatusDto.DeviceId ?? string.Empty, input.Status, input.UnreadCount);
    }

    public async Task ExitWallet()
    {
        var deviceId = _connectionProvider.GetConnectionByConnectionId(Context.ConnectionId)?.ClientId;
        if (!CheckDeviceId(deviceId))
        {
            return;
        }

        var userId = CurrentUser.GetId();
        await _deviceInfoReportAppService.ExitWalletAsync(deviceId, CurrentUser.GetId());
        _logger.LogInformation("ExitWallet, deviceId:{deviceId}, userId:{userId}", deviceId, userId);
    }

    public async Task SwitchNetwork()
    {
        if (!CheckIsOpen())
        {
            return;
        }
        
        var deviceId = _connectionProvider.GetConnectionByConnectionId(Context.ConnectionId)?.ClientId;
        if (!CheckDeviceId(deviceId))
        {
            return;
        }

        var userId = CurrentUser.GetId();
        await _deviceInfoReportAppService.SwitchNetworkAsync(Context.ConnectionId, CurrentUser.GetId());
        _logger.LogInformation("SwitchNetwork, deviceId:{deviceId}, userId:{userId}", deviceId, userId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var deviceId = _connectionProvider.GetConnectionByConnectionId(Context.ConnectionId)?.ClientId;
        _hubService.UnRegisterClient(Context.ConnectionId);
        
        if (!CheckIsOpen())
        {
            return;
        }
        await _deviceInfoReportAppService.OnDisconnectedAsync(deviceId, CurrentUser.GetId());
        _logger.LogInformation("disconnected, clientId:{clientId}", deviceId ?? "");
    }

    private bool CheckIsOpen() => _messagePushOptions.IsOpen;

    private bool CheckDeviceId(string deviceId)
    {
        if (!deviceId.IsNullOrWhiteSpace())
        {
            return true;
        }

        _logger.LogWarning("device id is empty, userId:{userId}", CurrentUser.GetId());
        return false;
    }
}