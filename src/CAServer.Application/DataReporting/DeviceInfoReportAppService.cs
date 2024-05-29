using System;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.DataReporting.Dtos;
using CAServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.DataReporting;

[RemoteService(false), DisableAuditing]
public class DeviceInfoReportAppService : CAServerAppService, IDeviceInfoReportAppService
{
    private readonly IMessagePushRequestProvider _requestProvider;
    private readonly HostInfoOptions _options;

    public DeviceInfoReportAppService(IMessagePushRequestProvider requestProvider,
        IOptionsSnapshot<HostInfoOptions> options)
    {
        _requestProvider = requestProvider;
        _options = options.Value;
    }

    public async Task ReportDeviceInfoAsync(UserDeviceReportingDto input)
    {
        input.NetworkType = _options.Network;
        await _requestProvider.PostAsync(MessagePushConstant.ReportDeviceInfoUri, input);
        Logger.LogDebug("report deviceInfo, userId: {userId}, deviceId: {deviceId}", input.UserId, input.DeviceId);
    }

    public async Task ReportAppStatusAsync(AppStatusReportingDto input)
    {
        input.NetworkType = _options.Network;
        await _requestProvider.PostAsync(MessagePushConstant.ReportAppStatusUri, input);
    }

    public async Task ExitWalletAsync(string deviceId, Guid userId)
    {
        await _requestProvider.PostAsync(MessagePushConstant.ExitWalletUri, new { userId, deviceId, _options.Network });
        Logger.LogDebug("exitWallet, userId: {userId}, deviceId: {deviceId}", userId, deviceId);
    }

    public async Task SwitchNetworkAsync(string deviceId, Guid userId)
    {
        // off line
        await _requestProvider.PostAsync(MessagePushConstant.SwitchNetworkUri,
            new { userId, deviceId, networkType = _options.Network });
        Logger.LogDebug("switchNetwork, userId: {userId}, deviceId: {deviceId}", userId, deviceId);
    }

    public async Task OnDisconnectedAsync(string deviceId, Guid userId)
    {
        await _requestProvider.PostAsync(MessagePushConstant.SwitchNetworkUri,
            new { userId, deviceId, networkType = _options.Network });
        Logger.LogDebug("disconnected, userId: {userId}, deviceId: {deviceId}", userId, deviceId);
    }
}