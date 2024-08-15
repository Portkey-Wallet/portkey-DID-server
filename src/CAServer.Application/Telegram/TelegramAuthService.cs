using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
public class TelegramAuthService : CAServerAppService, ITelegramAuthService
{
    private readonly ILogger<TelegramAuthService> _logger;
    private readonly TelegramAuthOptions _telegramAuthOptions;
    private readonly IObjectMapper _objectMapper;
    private readonly IHttpClientService _httpClientService;
    private readonly TelegramVerifierOptions _telegramVerifierOptions;

    public TelegramAuthService(ILogger<TelegramAuthService> logger,
        IOptionsSnapshot<TelegramAuthOptions> telegramAuthOptions, IObjectMapper objectMapper,
        IHttpClientService httpClientService, IOptions<TelegramVerifierOptions> telegramVerifierOptions)
    {
        _logger = logger;
        _telegramAuthOptions = telegramAuthOptions.Value;
        _objectMapper = objectMapper;
        _httpClientService = httpClientService;
        _telegramVerifierOptions = telegramVerifierOptions.Value;
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

    public async Task<TelegramAuthResponseDto<TelegramBotInfoDto>> RegisterTelegramBot(RegisterTelegramBotDto request)
    {
        var checkResult = CheckSecret(request);
        if (!checkResult.Success)
        {
            return checkResult;
        }
        var url = $"{_telegramVerifierOptions.Url}/api/app/auth/bot/register";
        var resultDto = await _httpClientService.PostAsync<ResponseResultDto<TelegramBotInfoDto>>(url, request);
        _logger.LogInformation("RegisterTelegramBot url:{0} params:{1} response:{2}",
            url, JsonConvert.SerializeObject(resultDto), resultDto == null ? null : JsonConvert.SerializeObject(resultDto));
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
        _logger.LogInformation("==================secrets:{0}", JsonConvert.SerializeObject(secrets));
        if (secrets.Length != 2)
        {
            return new TelegramAuthResponseDto<TelegramBotInfoDto>
            {
                Success = false,
                Message = "Secret Format is invalid"
            };
        }
        if (secrets[0].IsNullOrEmpty() || !Regex.IsMatch(secrets[0], @"^\d+$")
            || secret[0].ToString().Length <= _telegramVerifierOptions.BotIdMinimumLength
            || secret[0].ToString().Length >= _telegramVerifierOptions.BotIdMaximumLength)
        {
            return new TelegramAuthResponseDto<TelegramBotInfoDto>
            {
                Success = false,
                Message = "Secret Format is invalid, botId is invalid"
            };
        }

        if (secrets[1].IsNullOrEmpty() || !Regex.IsMatch(secrets[1], @"^[a-zA-Z0-9_-]+$")
            || secret[1].ToString().Length <= _telegramVerifierOptions.SecretMinimumLength
            || secret[1].ToString().Length >= _telegramVerifierOptions.SecretMaximumLength)
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