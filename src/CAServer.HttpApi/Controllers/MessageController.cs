using System.Threading.Tasks;
using CAServer.Message;
using CAServer.Message.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Message")]
[Route("api/app/message")]
public class MessageController : CAServerController
{
    private readonly IMessageAppService _messageAppService;

    public MessageController(IMessageAppService messageAppService)
    {
        _messageAppService = messageAppService;
    }

    //[Authorize]
    [HttpPost("scanLoginSuccess")]
    public async Task ScanLoginSuccess(ScanLoginDto request)
    {
        await _messageAppService.ScanLoginSuccess(request);
    }
}