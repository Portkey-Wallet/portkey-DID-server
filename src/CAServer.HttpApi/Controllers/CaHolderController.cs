using System.Threading.Tasks;
using CAServer.CaHolder;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("CaHolder")]
[Route("api/app/holder/")]
public class CaHolderController : CAServerController
{
    private readonly ICaHolderAppService _caHolderAppService;

    public CaHolderController(ICaHolderAppService caHolderAppService)
    {
        _caHolderAppService = caHolderAppService;
    }

    [HttpGet("statistic")]
    public async Task<string> Statistic()
    {
        return await _caHolderAppService.Statistic();
    }
    
    [HttpGet("sort")]
    public async Task<string> Sort()
    {
        return await _caHolderAppService.Sort();
    }
}