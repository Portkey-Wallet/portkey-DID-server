using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.Tab;
using CAServer.Tab.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Tab")]
[Route("api/app/tab")]
public class TabController : CAServerController
{
    private readonly ITabAppService _tabAppService;
    public TabController(ITabAppService tabAppService)
    {
        _tabAppService = tabAppService;
    }

    [HttpPost("complete")]
    public async Task CompleteAsync(TabCompleteDto input)
    {
        await _tabAppService.CompleteAsync(input);
    }
}