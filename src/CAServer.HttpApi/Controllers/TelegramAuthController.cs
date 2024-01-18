using System;
using System.Threading.Tasks;
using CAServer.Switch;
using CAServer.Telegram;
using CAServer.Telegram.Dtos;
using CAServer.Telegram.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("TelegramAuth")]
[Route("api/app/telegramAuth/")]
public class TelegramAuthController : CAServerController
{
    private readonly ILogger<TelegramAuthController> _logger;
    private readonly ITelegramAuthService _telegramAuthService;
    private readonly TelegramAuthOptions _telegramAuthOptions;
    private readonly ISwitchAppService _switchAppService;

    private const string TelegramLoginSwitch = "TelegramLogin";

    public TelegramAuthController(ILogger<TelegramAuthController> logger, ITelegramAuthService telegramAuthService,
        IOptions<TelegramAuthOptions> telegramAuthOptions, ISwitchAppService switchAppService)
    {
        _logger = logger;
        _telegramAuthService = telegramAuthService;
        _telegramAuthOptions = telegramAuthOptions.Value;
        _switchAppService = switchAppService;
    }

    [HttpGet("getTelegramBot")]
    public async Task<TelegramBotDto> GetTelegramBotAsync()
    {
        if (!_switchAppService.GetSwitchStatus(TelegramLoginSwitch).IsOpen)
        {
            throw new UserFriendlyException("Telegram login not supported");
        }

        return await _telegramAuthService.GetTelegramBotInfoAsync();
    }

    [HttpGet("receive/{redirect}")]
    public async Task<IActionResult> ReceiveAsync(string redirect, TelegramAuthReceiveRequest request)
    {
        if (!_switchAppService.GetSwitchStatus(TelegramLoginSwitch).IsOpen)
        {
            throw new UserFriendlyException("Telegram login not supported");
        }
        
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