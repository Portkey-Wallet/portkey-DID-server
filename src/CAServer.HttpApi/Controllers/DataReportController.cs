using System.Threading.Tasks;
using CAServer.DataReporting.Dtos;
using CAServer.DataReporting.Etos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("DataReport")]
[Route("api/app/report")]
public class DataReportController : CAServerController
{
    private readonly IDistributedEventBus _eventBus;

    public DataReportController(IDistributedEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    [HttpPost("exitWallet"), Authorize]
    public async Task ExitWalletAsync(ExitWalletDto exitWalletDto)
    {
        await _eventBus.PublishAsync(new ExitWalletEto
        {
            UserId = CurrentUser.GetId(),
            DeviceId = exitWalletDto.DeviceId
        });
    }
}