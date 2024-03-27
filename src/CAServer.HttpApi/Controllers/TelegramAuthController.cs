using System;
using System.Threading.Tasks;
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

    public TelegramAuthController(ILogger<TelegramAuthController> logger, ITelegramAuthService telegramAuthService,
        IOptions<TelegramAuthOptions> telegramAuthOptions)
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

    [HttpGet("token")]
    public async Task<TelegramAuthTokenResponseDto> TokenAsync(TelegramAuthReceiveRequest request)
    {
        request = new TelegramAuthReceiveRequest
        {
            Id = "5990848037",
            UserName = null,
            Auth_Date = "1712528610",
            First_Name = "Aurora",
            Last_Name = null,
            Hash = "a968a40b2f412a317ed13b0814e682ce03498e781e9719e1b674be88ebc1cb0f",
            Photo_Url = null
        };

        var token = await _telegramAuthService.ValidateTelegramHashAndGenerateTokenAsync(request);

        return new TelegramAuthTokenResponseDto
        {
            Token = token
        };
    }
}