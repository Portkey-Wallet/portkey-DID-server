using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Telegram.Dtos;
using CAServer.Telegram.Options;
using CAServer.Verifier;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace CAServer.Telegram;

[RemoteService(false), DisableAuditing]
public partial class TelegramAuthService : CAServerAppService, ITelegramAuthService
{
    [GeneratedRegex("^\\d+$")]
    private static partial Regex DigitRegex();
    [GeneratedRegex("^[a-zA-Z0-9_-]+$")]
    private static partial Regex DigitLetterDashUnderScoreRegex();
    private readonly ILogger<TelegramAuthService> _logger;
    private readonly TelegramAuthOptions _telegramAuthOptions;
    private readonly IObjectMapper _objectMapper;
    private readonly IHttpClientService _httpClientService;
    private readonly TelegramVerifierOptions _telegramVerifierOptions;
    private readonly ITelegramRateLimiter _telegramRateLimiter;

    public TelegramAuthService(ILogger<TelegramAuthService> logger,
        IOptionsSnapshot<TelegramAuthOptions> telegramAuthOptions, IObjectMapper objectMapper,
        IHttpClientService httpClientService, IOptions<TelegramVerifierOptions> telegramVerifierOptions,
        ITelegramRateLimiter telegramRateLimiter)
    {
        _logger = logger;
        _telegramAuthOptions = telegramAuthOptions.Value;
        _objectMapper = objectMapper;
        _httpClientService = httpClientService;
        _telegramVerifierOptions = telegramVerifierOptions.Value;
        _telegramRateLimiter = telegramRateLimiter;
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
    private async Task<T> RequestAsync<T>(Func<Task<T>> task)
    {
        await _telegramRateLimiter.RecordRequestAsync();
        return await task();
    }

    public async Task<TelegramAuthResponseDto<TelegramBotInfoDto>> RegisterTelegramBot(RegisterTelegramBotDto request)
    {
        return await RequestAsync(async () => await DoRegisterTelegramBot(request));
    }

    private async Task<TelegramAuthResponseDto<TelegramBotInfoDto>> DoRegisterTelegramBot(RegisterTelegramBotDto request)
    {
        var checkResult = CheckSecret(request);
        if (!checkResult.Success)
        {
            return checkResult;
        }
        var url = $"{_telegramVerifierOptions.Url}/api/app/auth/bot/register";
        var resultDto = await _httpClientService.PostAsync<ResponseResultDto<TelegramBotInfoDto>>(url, request);
        if (resultDto == null)
        {
            return new TelegramAuthResponseDto<TelegramBotInfoDto>
            {
                Success = false,
                Message = "no result returned"
            };
        }

        if (!resultDto.Success)
        {
            return new TelegramAuthResponseDto<TelegramBotInfoDto>
            {
                Success = false,
                Message = resultDto.Message
            };
        }

        return new TelegramAuthResponseDto<TelegramBotInfoDto>
        {
            Success = resultDto.Success,
            Message = resultDto.Message,
            Data = resultDto.Data
        };
    }

    private TelegramAuthResponseDto<TelegramBotInfoDto> CheckSecret(RegisterTelegramBotDto request)
    {
        if (request == null || request.Secret.IsNullOrEmpty())
        {
            return new TelegramAuthResponseDto<TelegramBotInfoDto>
            {
                Success = false,
                Message = "Invalid input, secret needs"
            };
        }

        var secret = request.Secret;
        if (!secret.Contains(CommonConstant.Colon))
        {
            return new TelegramAuthResponseDto<TelegramBotInfoDto>
            {
                Success = false,
                Message = "Secret Format is invalid"
            };
        }

        var secrets = secret.Split(CommonConstant.Colon);
        if (secrets.Length != 2)
        {
            return new TelegramAuthResponseDto<TelegramBotInfoDto>
            {
                Success = false,
                Message = "Secret Format is invalid"
            };
        }

        var botId = secrets[0];
        if (botId.IsNullOrEmpty()
            || !DigitRegex().IsMatch(botId)
            || botId.Length <= _telegramVerifierOptions.BotIdMinimumLength
            || botId.Length >= _telegramVerifierOptions.BotIdMaximumLength)
        {
            return new TelegramAuthResponseDto<TelegramBotInfoDto>
            {
                Success = false,
                Message = "Secret Format is invalid, botId is invalid"
            };
        }

        var token = secrets[1];
        if (token.IsNullOrEmpty()
            || !DigitLetterDashUnderScoreRegex().IsMatch(token)
            || token.Length <= _telegramVerifierOptions.SecretMinimumLength
            || token.Length >= _telegramVerifierOptions.SecretMaximumLength)
        {
            return new TelegramAuthResponseDto<TelegramBotInfoDto>
            {
                Success = false,
                Message = "Secret Content Format is invalid"
            };
        }
        return new TelegramAuthResponseDto<TelegramBotInfoDto>
        {
            Success = true,
        };
    }
}