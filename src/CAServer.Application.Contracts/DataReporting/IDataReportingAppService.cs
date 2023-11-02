using System;
using System.Threading.Tasks;
using CAServer.DataReporting.Dtos;

namespace CAServer.DataReporting;

public interface IDataReportingAppService
{
    Task ReportDeviceInfoAsync(UserDeviceReporting input);
    Task ReportAppStatusAsync(AppStatusReporting input);
    Task ExitWalletAsync(string deviceId, Guid userId);
    Task SwitchNetworkAsync(string deviceId, Guid userId);
}