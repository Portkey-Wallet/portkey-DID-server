using System.Threading.Tasks;
using CAServer.Tab;
using CAServer.Tab.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Hub")]
[Route("api/app/hub")]
public class HubController: CAServerController
{
    private readonly ITabAppService _tabAppService;
    public HubController(ITabAppService tabAppService)
    {
        _tabAppService = tabAppService;
    }

    [HttpPost("send")]
    public async Task CompleteAsync(TabCompleteDto input)
    {
        await _tabAppService.CompleteAsync(input);
    }
}