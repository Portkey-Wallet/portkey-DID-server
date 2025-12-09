using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Telegram.Dtos;

namespace CAServer.Telegram;


public interface  ITelegramAuthService
{
    Task<TelegramBotDto> GetTelegramBotInfoAsync();

    Task<string> ValidateTelegramHashAndGenerateTokenAsync(TelegramAuthReceiveRequest telegramAuthReceiveRequest);
    
    Task<string> ValidateTelegramHashAndGenerateTokenAsync(IDictionary<string, string> requestParameter);
    
    Task<TelegramAuthResponseDto<TelegramBotInfoDto>> RegisterTelegramBot(RegisterTelegramBotDto request);
}