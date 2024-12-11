using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.FreeMint;
using CAServer.FreeMint.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("FreeMint")]
[Route("api/app/mint/")]
[Authorize]
[IgnoreAntiforgeryToken]
public class FreeMintController : CAServerController
{
    private readonly IFreeMintAppService _freeMintAppService;

    public FreeMintController(IFreeMintAppService freeMintAppService)
    {
        _freeMintAppService = freeMintAppService;
    }

    [HttpGet("recentStatus")]
    public async Task<GetRecentStatusDto> GetRecentStatusAsync()
    {
        return await _freeMintAppService.GetRecentStatusAsync();
    }
    
    [HttpGet("info")]
    public async Task<GetMintInfoDto> GetMintInfoAsync()
    {
        return await _freeMintAppService.GetMintInfoAsync();
    }
    
    [HttpPost("confirm")]
    public async Task<ConfirmDto> ConfirmAsync(ConfirmRequestDto requestDto)
    {
        return await _freeMintAppService.ConfirmAsync(requestDto);
    }

    [HttpGet("status")]
    public async Task<GetStatusDto> GetStatusAsync(string itemId)
    {
        return await _freeMintAppService.GetStatusAsync(itemId);
    }
    
    
    [HttpGet("itemInfo")]
    public async Task<GetItemInfoDto> GetItemInfoAsync(string itemId)
    {
        return await _freeMintAppService.GetItemInfoAsync(itemId);
    }
}