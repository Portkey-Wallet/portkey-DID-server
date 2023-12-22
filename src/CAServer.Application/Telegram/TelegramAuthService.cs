using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CAServer.Telegram.Dtos;
using CAServer.Telegram.Options;
using CAServer.Telegram.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace CAServer.Telegram;

public class TelegramAuthService : CAServerAppService, ITelegramAuthService
{
    private readonly ILogger<TelegramAuthService> _logger;
    private readonly TelegramAuthOptions _telegramAuthOptions;
    private readonly IObjectMapper _objectMapper;
    private readonly ITelegramAuthProvider _telegramAuthProvider;
    private readonly IJwtTokenProvider _jwtTokenProvider;
    
    private const string Pattern = @"(\\w+)#(\\w+)=(\\w+)";

    public TelegramAuthService(ILogger<TelegramAuthService> logger,
        IOptionsSnapshot<TelegramAuthOptions> telegramAuthOptions, IObjectMapper objectMapper,
        ITelegramAuthProvider telegramAuthProvider, IJwtTokenProvider jwtTokenProvider)
    {
        _logger = logger;
        _telegramAuthOptions = telegramAuthOptions.Value;
        _objectMapper = objectMapper;
        _telegramAuthProvider = telegramAuthProvider;
        _jwtTokenProvider = jwtTokenProvider;
    }
    
    public Task<Tuple<string, string>>  GetTelegramAuthResultAsync(string param)
    {
        Match match = Regex.Match(param, Pattern);
        if (match.Success)
        {
            return Task.FromResult(new Tuple<string, string>(match.Groups[1].Value, match.Groups[3].Value));
        }
        _logger.LogInformation("telegram auth result is valid");
        throw new UserFriendlyException("Invalid Telegram Login Information");
    }

    public Task<TelegramBotDto> GetTelegramBotInfoAsync()
    {
        return Task.FromResult(new TelegramBotDto()
        {
            BotId = _telegramAuthOptions.Bots[_telegramAuthOptions.DefaultUsed ?? ""]?.BotId,
            BotName = _telegramAuthOptions.Bots[_telegramAuthOptions.DefaultUsed ?? ""]?.BotName
        });
    }

    public async Task<string> ValidateTelegramHashAndGenerateTokenAsync(TelegramAuthReceiveRequest request)
    {
        if (request == null)
        {
            _logger.LogInformation("telegram auth result is null");
            throw new UserFriendlyException("Invalid Telegram Login Information");
        }
        
        var telegramAuthDto = _objectMapper.Map<TelegramAuthReceiveRequest, TelegramAuthDto>(request);
        if (!await _telegramAuthProvider.ValidateTelegramHashAsync(telegramAuthDto))
        {
            _logger.LogError("Invalid Telegram Login Information, id={0}", telegramAuthDto.Id);
            throw new UserFriendlyException("Invalid Telegram Login Information");
        }

        return await _jwtTokenProvider.GenerateTokenAsync(new Dictionary<string, string>()
        {
            { TelegramTokenClaimNames.UserId, telegramAuthDto.Id },
            { TelegramTokenClaimNames.UserName, telegramAuthDto.UserName },
            { TelegramTokenClaimNames.AuthDate, telegramAuthDto.AuthDate },
            { TelegramTokenClaimNames.FirstName, telegramAuthDto.FirstName },
            { TelegramTokenClaimNames.LastName, telegramAuthDto.LastName },
            { TelegramTokenClaimNames.Hash, telegramAuthDto.Hash },
            { TelegramTokenClaimNames.ProtoUrl, telegramAuthDto.ProtoUrl }
        });
    }
}