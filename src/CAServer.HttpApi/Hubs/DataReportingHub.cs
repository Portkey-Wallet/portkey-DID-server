using System;
using System.Threading.Tasks;
using CAServer.DataReporting;
using CAServer.DataReporting.Dtos;
using CAServer.Hub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
    private readonly IDataReportingAppService _dataReportingAppService;
    private readonly IObjectMapper _objectMapper;
    private readonly IConnectionProvider _connectionProvider;

    public DataReportingHub(ILogger<DataReportingHub> logger,
        IHubService hubService,
        IDataReportingAppService dataReportingAppService,
        IObjectMapper objectMapper,
        IConnectionProvider connectionProvider)
    {
        _logger = logger;
        _hubService = hubService;
        _dataReportingAppService = dataReportingAppService;
        _objectMapper = objectMapper;
        _connectionProvider = connectionProvider;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("connected!!!!");

        return base.OnConnectedAsync();
    }

    public async Task Connect(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            return;
        }

        var id = CurrentUser.Id;
        await _hubService.RegisterClient(clientId, Context.ConnectionId);
        _logger.LogInformation("clientId={ClientId} connect, userId:{userId}", clientId, id);
    }

    public async Task ReportDeviceInfo(UserDeviceReportingRequestDto input)
    {
        var dto = _objectMapper.Map<UserDeviceReportingRequestDto, UserDeviceReportingDto>(input);
        dto.UserId = CurrentUser.GetId();
        _logger.LogInformation("report DeviceInfo, {data}", JsonConvert.SerializeObject(dto));
        await _dataReportingAppService.ReportDeviceInfoAsync(dto);
    }

    public async Task ReportAppStatus(AppStatusReportingRequestDto input)
    {
        var dto = _objectMapper.Map<AppStatusReportingRequestDto, AppStatusReportingDto>(input);
        dto.UserId = CurrentUser.GetId();
        dto.DeviceId = _connectionProvider.GetConnectionByConnectionId(Context.ConnectionId)?.ClientId;

        _logger.LogInformation("report status, {data}", JsonConvert.SerializeObject(dto));
        await _dataReportingAppService.ReportAppStatusAsync(dto);
    }

    public async Task ExitWallet()
    {
        var deviceId = _connectionProvider.GetConnectionByConnectionId(Context.ConnectionId)?.ClientId;
        if (deviceId.IsNullOrWhiteSpace())
        {
            return;
        }

        var userId = CurrentUser.GetId();
        await _dataReportingAppService.ExitWalletAsync(Context.ConnectionId, CurrentUser.GetId());
        _logger.LogInformation("ExitWallet, deviceId:{deviceId}, userId:{userId}", deviceId, userId);
    }

    public async Task SwitchNetwork()
    {
        var deviceId = _connectionProvider.GetConnectionByConnectionId(Context.ConnectionId)?.ClientId;
        if (deviceId.IsNullOrWhiteSpace())
        {
            return;
        }

        var userId = CurrentUser.GetId();
        await _dataReportingAppService.ExitWalletAsync(Context.ConnectionId, CurrentUser.GetId());
        _logger.LogInformation("SwitchNetwork, deviceId:{deviceId}, userId:{userId}", deviceId, userId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        // offline, can I get userid if disconnected???
        var userId = CurrentUser.GetId();
        await ReportAppStatus(new AppStatusReportingRequestDto()
        {
            Status = AppStatus.Offline
        });

        var clientId = _connectionProvider.GetConnectionByConnectionId(Context.ConnectionId)?.ClientId;
        _hubService.UnRegisterClient(Context.ConnectionId);
        _logger.LogInformation("disconnect, clientId:{clientId}", clientId ?? "");
    }
}