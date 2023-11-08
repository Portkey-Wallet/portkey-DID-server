using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CAServer.Switch;
using CAServer.Switch.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Switch")]
[Route("api/app/switch")]
public class SwitchController : CAServerController
{
    private readonly ISwitchAppService _switchAppService;

    public SwitchController(ISwitchAppService switchAppService)
    {
        _switchAppService = switchAppService;
    }

    [HttpGet]
    public SwitchDto GetSwitchStatus([Required] string switchName) => _switchAppService.GetSwitchStatus(switchName).Result;
    
    [HttpGet("GetSwitchStatus2")]
    public async Task GetSwitchStatus2() => await _switchAppService.GetSwitchStatus2();
}