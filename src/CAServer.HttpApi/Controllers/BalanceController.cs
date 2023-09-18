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
}