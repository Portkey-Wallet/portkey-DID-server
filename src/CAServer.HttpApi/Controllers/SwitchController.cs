using System.Threading.Tasks;
using CAServer.Notify;
using CAServer.Switch;
using CAServer.Switch.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Switch")]
[Route("api/app/switch")]
public class SwitchController
{
    private readonly ISwitchAppService _switchAppService;

    public SwitchController(ISwitchAppService switchAppService)
    {
        _switchAppService = switchAppService;
    }

    [HttpGet("ramp")]
    public RampSwitchDto GetSwitchStatus() => _switchAppService.GetSwitchStatus();
}