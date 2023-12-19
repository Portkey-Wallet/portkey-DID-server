using System.Threading.Tasks;
using CAServer.Telegram;
using CAServer.Telegram.Dtos;
using CAServer.Telegram.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("AppleAuth")]
[Route("api/app/telegramAuth/")]
public class TelegramAuthController : CAServerController
{
    private readonly ITelegramAuthService _telegramAuthService;
    private readonly TelegramAuthOptions _telegramAuthOptions;

    public TelegramAuthController(ITelegramAuthService telegramAuthService, IOptions<TelegramAuthOptions> telegramAuthOptions)
    {
        _telegramAuthService = telegramAuthService;
        _telegramAuthOptions = telegramAuthOptions.Value;
    }

    [HttpGet("getTelegramBot")]
    public async Task<TelegramBotDto> GetTelegramBotAsync()
    {
        return await _telegramAuthService.GetTelegramBotInfoAsync();
    }

    [HttpPost("receive")]
    public async Task<IActionResult> ReceiveAsync(TelegramAuthReceiveRequest request)
    {
        var token = await _telegramAuthService.ValidateTelegramHashAndGenerateTokenAsync(request);
        return Redirect($"{_telegramAuthOptions.RedirectUrl}?token={token}");
    }
}