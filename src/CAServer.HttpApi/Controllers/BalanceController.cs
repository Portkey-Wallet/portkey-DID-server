using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Balance;
using CAServer.Bookmark;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Balance")]
[Route("api/app/balance/")]
[IgnoreAntiforgeryToken]
public class BalanceController : CAServerController
{
    private readonly IBalanceAppService _balanceAppService;

    public BalanceController(IBalanceAppService balanceAppService)
    {
        _balanceAppService = balanceAppService;
    }

    [HttpGet]
    public async Task<string> GetAsync(string chainId)
    {
         await _balanceAppService.GetBalanceInfoAsync(chainId);
         return "ok";
    }
    
    [HttpGet("getAddress")]
    public async Task<string> GetAddressAsync()
    {
        await _balanceAppService.Statistic();
        return "ok";
    }

    [HttpGet("activities")]
    public async Task<Dictionary<string, int>> GetActivityCountByDayAsync()
    {
        return await _balanceAppService.GetActivityCountByDayAsync();
    }
}