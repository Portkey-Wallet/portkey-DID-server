using System;
using System.Collections.Generic;
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
        Logger.LogDebug("reportDeviceInfo, userId: {userId}, deviceId: {deviceId}, data: {data}", input.UserId,
            input.DeviceId, JsonConvert.SerializeObject(input));
        input.NetworkType = _options.Network;

        await _requestProvider.PostAsync(MessagePushConstant.ReportDeviceInfoUri, input);
    }

    public async Task ReportAppStatusAsync(AppStatusReportingDto input)
    {
        Logger.LogDebug("reportAppStatus, userId: {userId}, deviceId: {deviceId}, status: {status}", input.UserId,
            input.DeviceId, input.Status.ToString());
        input.NetworkType = _options.Network;

        await _requestProvider.PostAsync(MessagePushConstant.ReportAppStatusUri, input);
    }

    public async Task ExitWalletAsync(string deviceId, Guid userId)
    {
        // delete
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
}