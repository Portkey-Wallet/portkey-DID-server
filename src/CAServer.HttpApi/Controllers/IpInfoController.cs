using System.Collections.Generic;
using System.Threading.Tasks;
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

    [HttpGet("ipInfo2")]
    public async Task<List<UpdateXpScoreRepairDataDto>> GetIpInfo2Async()
    {
        return await _ipInfoAppService.GetIpInfo2Async();
    }

    [HttpGet("updateScore")]
    public async Task UpdateRepairScore()
    {
        await _ipInfoAppService.UpdateRepairScore();
    }
}