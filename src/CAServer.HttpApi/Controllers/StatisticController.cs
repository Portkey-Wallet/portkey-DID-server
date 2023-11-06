using System.Threading.Tasks;
using CAServer.Statistic;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Statistic")]
[Route("api/app/statistic")]
[IgnoreAntiforgeryToken]
public class StatisticController
{
    private readonly IStatisticAppService _statisticAppService;

    public StatisticController(IStatisticAppService statisticAppService)
    {
        _statisticAppService = statisticAppService;
    }

    [HttpGet]
    public async Task<int> GetTransferInfoAsync()
    {
        return await _statisticAppService.GetTransferInfoAsync();
    }
}