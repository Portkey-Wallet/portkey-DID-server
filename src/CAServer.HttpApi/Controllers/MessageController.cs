using System.Threading.Tasks;
using Asp.Versioning;
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

    [HttpPost("scanLoginSuccess")]
    public async Task ScanLoginSuccessAsync(ScanLoginDto request)
    {
        await _messageAppService.ScanLoginSuccessAsync(request);
    }
    
    [HttpPost("scanLogin")]
    public async Task ScanLoginAsync(ScanLoginDto request)
    {
        await _messageAppService.ScanLoginAsync(request);
    }
}