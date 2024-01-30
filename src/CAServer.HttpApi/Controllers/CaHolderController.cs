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
    
    [HttpGet("statistic2")]
    public async Task<string> Statistic2()
    {
        return await _caHolderAppService.Statistic2();
    }
    
    [HttpGet("sort")]
    public async Task<string> Sort()
    {
        return await _caHolderAppService.Sort();
    }
        
    [HttpGet("term")]
    public async Task<string> AddTerm()
    {
        return await _caHolderAppService.Term();
    }
}