using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.Hubs;
using Microsoft.AspNetCore.Mvc;

namespace CAServer.Controllers;

[Area("app")]
[ControllerName("CAHub")]
[Route("api/app/account/hub")]
public class CAHubController : CAServerController
{
    private readonly IHubService _hubService;

    public CAHubController(IHubService hubService)
    {
        _hubService = hubService;
    }

    [HttpPost("ping")]
    public async Task<string> Ping([FromBody] HubPingRequestDto requestDto)
    {
        await _hubService.Ping(requestDto.Context, requestDto.Content);
        return "OK";
    }

    [HttpPost("response")]
    public async Task<HubResponse<object>> GetResponse([FromBody] GetHubRequestDto requestDto)
    {
        return await _hubService.GetResponse(requestDto.Context);
    }
}