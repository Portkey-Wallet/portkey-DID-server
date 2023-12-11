using System;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.DataReporting.Dtos;
using CAServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.DataReporting;

[RemoteService(false), DisableAuditing]
public class DataReportingAppService : CAServerAppService, IDataReportingAppService
{
    private readonly IMessagePushRequestProvider _requestProvider;
    private readonly HostInfoOptions _options;

    public DataReportingAppService(IMessagePushRequestProvider requestProvider,
        IOptionsSnapshot<HostInfoOptions> options)
    {
        _requestProvider = requestProvider;
        _options = options.Value;
    }

    public async Task ReportDeviceInfoAsync(UserDeviceReportingDto input)
    {
        input.NetworkType = _options.Network;
        await _requestProvider.PostAsync(MessagePushConstant.ReportDeviceInfoUri, input);
    }

    public async Task ReportAppStatusAsync(AppStatusReportingDto input)
    {
        input.NetworkType = _options.Network;
        await _requestProvider.PostAsync(MessagePushConstant.ReportAppStatusUri, input);
    }

    public async Task ExitWalletAsync(string deviceId, Guid userId)
    {
        Logger.LogDebug("exitWallet, userId: {userId}, deviceId: {deviceId}", userId, deviceId);
        await _requestProvider.PostAsync(MessagePushConstant.ExitWalletUri, new { userId, deviceId, _options.Network });
    }

    public async Task SwitchNetworkAsync(string deviceId, Guid userId)
    {
        // off line
        Logger.LogDebug("switchNetwork, userId: {userId}, deviceId: {deviceId}", userId, deviceId);
        await _requestProvider.PostAsync(MessagePushConstant.SwitchNetworkUri,
            new { userId, deviceId, networkType = _options.Network });
    }

    public async Task OnDisconnectedAsync(string deviceId, Guid userId)
    {
        Logger.LogDebug("disconnected, userId: {userId}, deviceId: {deviceId}", userId, deviceId);
        await _requestProvider.PostAsync(MessagePushConstant.SwitchNetworkUri,
            new { userId, deviceId, networkType = _options.Network });
    }
}