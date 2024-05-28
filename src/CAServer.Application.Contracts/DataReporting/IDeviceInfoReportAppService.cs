using System;
using System.Threading.Tasks;
using CAServer.DataReporting.Dtos;

namespace CAServer.DataReporting;

public interface IDeviceInfoReportAppService
{
    Task ReportDeviceInfoAsync(UserDeviceReportingDto input);
    Task ReportAppStatusAsync(AppStatusReportingDto input);
    Task ExitWalletAsync(string deviceId, Guid userId);
    Task SwitchNetworkAsync(string deviceId, Guid userId);
    Task OnDisconnectedAsync(string deviceId, Guid userId);
}