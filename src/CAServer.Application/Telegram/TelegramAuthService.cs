using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Telegram.Dtos;
using CAServer.Telegram.Options;
using CAServer.Telegram.Provider;
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
    private readonly ITelegramAuthProvider _telegramAuthProvider;
    private readonly IJwtTokenProvider _jwtTokenProvider;

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

    public Task<TelegramBotDto> GetTelegramBotInfoAsync()
    {
        return Task.FromResult(new TelegramBotDto()
        {
            BotName = _telegramAuthOptions.Bots[_telegramAuthOptions.DefaultUsed ?? ""]?.BotName
        });
    }

    public async Task<string> ValidateTelegramHashAndGenerateTokenAsync(TelegramAuthReceiveRequest request)
    {
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