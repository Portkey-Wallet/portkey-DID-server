using System;
using System.Threading.Tasks;
using CAServer.DataReporting.Dtos;

namespace CAServer.DataReporting;

public interface IDataReportingAppService
{
    Task ReportDeviceInfoAsync(ReportingDto input);
    Task ReportAppStatusAsync(ReportingDataDto input);
    Task LogoutAsync(string deviceId,Guid userId);
}