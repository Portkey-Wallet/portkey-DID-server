using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.IpInfo;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("IpInfo")]
[Route("api/app/ipInfo/")]
public class IpInfoController : CAServerController
{
    private readonly IIpInfoAppService _ipInfoAppService;

    public IpInfoController(IIpInfoAppService ipInfoAppService)
    {
        _ipInfoAppService = ipInfoAppService;
    }

    [HttpGet("ipInfo")]
    public async Task<IpInfoResultDto> GetIpInfoAsync()
    {
        return await _ipInfoAppService.GetIpInfoAsync();
    }
}