using System;
using System.Threading.Tasks;
using CAServer.Telegram;
using CAServer.Telegram.Dtos;
using CAServer.Telegram.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<TelegramAuthController> _logger;
    private readonly ITelegramAuthService _telegramAuthService;
    private readonly TelegramAuthOptions _telegramAuthOptions;

    public TelegramAuthController(ILogger<TelegramAuthController> logger, ITelegramAuthService telegramAuthService, IOptions<TelegramAuthOptions> telegramAuthOptions)
    {
        _logger = logger;
        _telegramAuthService = telegramAuthService;
        _telegramAuthOptions = telegramAuthOptions.Value;
    }

    [HttpGet("getTelegramBot")]
    public async Task<TelegramBotDto> GetTelegramBotAsync()
    {
        return await _telegramAuthService.GetTelegramBotInfoAsync();
    }

    [HttpGet("receive/{redirect}")]
    public async Task<IActionResult> ReceiveAsync(string redirect, TelegramAuthReceiveRequest request)
    {
        var token = await _telegramAuthService.ValidateTelegramHashAndGenerateTokenAsync(request);
        var redirectUrl = _telegramAuthOptions.RedirectUrl[redirect];
        if (redirectUrl.IsNullOrWhiteSpace())
        {
            _logger.LogInformation("redirect page configuration error,redirect={0},url={1}", redirect, redirectUrl);
            throw new UserFriendlyException("Redirect Page Configuration Error");
        }
        return Redirect($"{redirectUrl}?token={token}&type=telegram");
    }
}