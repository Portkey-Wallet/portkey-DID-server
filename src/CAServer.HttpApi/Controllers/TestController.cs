using System.Threading.Tasks;
using CAServer.DataReporting;
using CAServer.DataReporting.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nest;
using Volo.Abp;
using Volo.Abp.Users;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Test")]
[Route("api/app/test")]
[Authorize]
[IgnoreAntiforgeryToken]
public class TestController : CAServerController
{
    private readonly IDataReportingAppService _dataReportingAppService;

    public TestController(IDataReportingAppService dataReportingAppService)
    {
        _dataReportingAppService = dataReportingAppService;
    }

    [HttpPost("reportDeviceInfo")]
    public async Task ReportDeviceInfo(UserDeviceReportingRequestDto input)
    {
        var reportingDto = ObjectMapper.Map<UserDeviceReportingRequestDto, UserDeviceReportingDto>(input);
        reportingDto.UserId = CurrentUser.GetId();

        await _dataReportingAppService.ReportDeviceInfoAsync(reportingDto);
    }

    [HttpPost("reportAppStatus")]
    public async Task ReportAppStatus(AppStatusReportingRequestDto input)
    {
        var reportingDto = ObjectMapper.Map<AppStatusReportingRequestDto, AppStatusReportingDto>(input);
        reportingDto.UserId = CurrentUser.GetId();
        reportingDto.DeviceId = reportingDto.UserId.ToString();

        await _dataReportingAppService.ReportAppStatusAsync(reportingDto);
    }

    [HttpPost("exitWallet")]
    public async Task ExitWallet(string deviceId)
    {
        await _dataReportingAppService.ExitWalletAsync(deviceId, CurrentUser.GetId());
    }
    
    [HttpPost("switch")]
    public async Task Switch(string deviceId)
    {
        await _dataReportingAppService.SwitchNetworkAsync(deviceId, CurrentUser.GetId());
    }
}