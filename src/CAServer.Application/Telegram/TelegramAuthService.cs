using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CAServer.Telegram.Dtos;
using CAServer.Telegram.Options;
using CAServer.Verifier;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace CAServer.Telegram;

public class TelegramAuthService : CAServerAppService, ITelegramAuthService
{
    private readonly ILogger<TelegramAuthService> _logger;
    private readonly TelegramAuthOptions _telegramAuthOptions;
    private readonly IObjectMapper _objectMapper;
    private readonly IHttpService _httpService;

    public TelegramAuthService(ILogger<TelegramAuthService> logger,
        IOptionsSnapshot<TelegramAuthOptions> telegramAuthOptions, IObjectMapper objectMapper,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _telegramAuthOptions = telegramAuthOptions.Value;
        _objectMapper = objectMapper;
        _httpService = new HttpService(telegramAuthOptions.Value.Timeout, httpClientFactory, true);
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
            _logger.LogInformation("telegram auth result is null");
            throw new UserFriendlyException("Invalid Telegram Login Information");
        }

        var telegramAuthDto = _objectMapper.Map<TelegramAuthReceiveRequest, TelegramAuthDto>(request);
        
        var url = $"{_telegramAuthOptions.BaseUrl}/api/app/auth/token";
        var properties = telegramAuthDto.GetType().GetProperties();
        var parameters = properties.ToDictionary(property => property.Name, property => property.GetValue(telegramAuthDto)?.ToString());

        var resultDto = await _httpService.PostResponseAsync<ResponseResultDto<string>>(url, parameters);

        if (resultDto == null || !resultDto.Success || resultDto.Data.IsNullOrWhiteSpace())
        {
            _logger.LogInformation("verification of the telegram information has failed, {0}", resultDto?.Message);
            throw new UserFriendlyException("Invalid Telegram Login Information");
        }

        return resultDto.Data;
    }
}