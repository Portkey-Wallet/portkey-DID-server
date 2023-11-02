using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.DataReporting.Dtos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.DataReporting;

[RemoteService(false), DisableAuditing]
public class DataReportingAppService : CAServerAppService, IDataReportingAppService
{
    private readonly IMessagePushRequestProvider _requestProvider;

    public DataReportingAppService(IMessagePushRequestProvider requestProvider)
    {
        _requestProvider = requestProvider;
    }

    public async Task ReportDeviceInfoAsync(UserDeviceReporting input)
    {
        Logger.LogDebug("reportDeviceInfo, userId: {userId}, deviceId: {deviceId}, data: {data}", input.UserId,
            input.DeviceId, JsonConvert.SerializeObject(input));
        await _requestProvider.PostAsync(MessagePushConstant.ReportDeviceInfoUri, input);
    }

    public async Task ReportAppStatusAsync(AppStatusReporting input)
    {
        Logger.LogDebug("reportAppStatus, userId: {userId}, deviceId: {deviceId}, status: {status}", input.UserId,
            input.DeviceId, input.Status.ToString());
        await _requestProvider.PostAsync(MessagePushConstant.ReportAppStatusUri, input);
    }

    public async Task ExitWalletAsync(string deviceId, Guid userId)
    {
        Logger.LogDebug("exitWallet, userId: {userId}, deviceId: {deviceId}", userId, deviceId);
        await _requestProvider.PostAsync(MessagePushConstant.ExitWalletUri, new { userId, deviceId });
    }

    public async Task SwitchNetworkAsync(string deviceId, Guid userId)
    {
        Logger.LogDebug("switchNetwork, userId: {userId}, deviceId: {deviceId}", userId, deviceId);
        await _requestProvider.PostAsync(MessagePushConstant.SwitchNetworkUri, new { userId, deviceId });
    }
}