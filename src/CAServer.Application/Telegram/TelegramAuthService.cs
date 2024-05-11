using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Telegram.Dtos;
using CAServer.Telegram.Options;
using CAServer.Verifier;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace CAServer.Telegram;

[RemoteService(false), DisableAuditing]
public class TelegramAuthService : CAServerAppService, ITelegramAuthService
{
    private readonly ILogger<TelegramAuthService> _logger;
    private readonly TelegramAuthOptions _telegramAuthOptions;
    private readonly IObjectMapper _objectMapper;
    private readonly IHttpClientService _httpClientService;

    public TelegramAuthService(ILogger<TelegramAuthService> logger,
        IOptionsSnapshot<TelegramAuthOptions> telegramAuthOptions, IObjectMapper objectMapper,
        IHttpClientService httpClientService)
    {
        _logger = logger;
        _telegramAuthOptions = telegramAuthOptions.Value;
        _objectMapper = objectMapper;
        _httpClientService = httpClientService;
    }

    public Task<TelegramBotDto> GetTelegramBotInfoAsync()
    {
        return Task.FromResult(new TelegramBotDto()
        {
            BotId = _telegramAuthOptions.BotId,
            BotName = _telegramAuthOptions.BotName
        });
    }

    public async Task<string> ValidateTelegramHashAndGenerateTokenAsync(TelegramAuthReceiveRequest request)
    {
        if (request == null || request.Id.IsNullOrWhiteSpace() || request.Hash.IsNullOrWhiteSpace())
        {
            _logger.LogInformation("Id or Hash is null");
            throw new UserFriendlyException("Invalid Telegram Login Information");
        }

        var telegramAuthDto = _objectMapper.Map<TelegramAuthReceiveRequest, TelegramAuthDto>(request);

        var url = $"{_telegramAuthOptions.BaseUrl}/api/app/auth/token";
        var properties = telegramAuthDto.GetType().GetProperties();
        var parameters = properties.ToDictionary(property => property.Name,
            property => property.GetValue(telegramAuthDto)?.ToString());

        var resultDto = await _httpClientService.PostAsync<ResponseResultDto<string>>(url, parameters);

        if (resultDto == null || !resultDto.Success || resultDto.Data.IsNullOrWhiteSpace())
        {
            _logger.LogInformation("verification of the telegram information has failed, {0}", resultDto?.Message);
            throw new UserFriendlyException("Invalid Telegram Login Information");
        }

        return resultDto.Data;
    }

    public async Task<string> ValidateTelegramHashAndGenerateTokenAsync(IDictionary<string, string> requestParameter)
    {
        if (requestParameter.IsNullOrEmpty())
        {
            _logger.LogInformation("`requestParameter` is null");
            throw new UserFriendlyException("Invalid Telegram Login Information");
        }
        var url = $"{_telegramAuthOptions.BaseUrl}/api/app/auth/bot/token";
        var resultDto = await _httpClientService.PostAsync<ResponseResultDto<string>>(url, requestParameter);
        if (resultDto == null || !resultDto.Success || resultDto.Data.IsNullOrWhiteSpace())
        {
            _logger.LogInformation("verification of the telegram information has failed, {0}", resultDto?.Message);
            throw new UserFriendlyException("Invalid Telegram Login Information");
        }

        return resultDto.Data;
    }
}