using System.Threading.Tasks;
using CAServer.Account;
using CAServer.GuardiansStatistic;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using GuardianType = Portkey.Contracts.CA.GuardianType;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("GuardiansStatistic")]
[Route("api/app/guardiansStatistic")]
public class GuardiansStatisticController : CAServerController
{
    private readonly IGuardiansStatisticAppService _guardiansStatistic;
    public GuardiansStatisticController(IGuardiansStatisticAppService guardiansStatistic)
    {
        _guardiansStatistic = guardiansStatistic;
    }

    [HttpGet("statistic")]
    public async Task<string> GetInfo()
    {
        return await _guardiansStatistic.GetInfo();
    }
}