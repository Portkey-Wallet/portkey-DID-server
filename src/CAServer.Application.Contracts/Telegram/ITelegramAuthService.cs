using System;
using System.Threading.Tasks;
using CAServer.Telegram.Dtos;

namespace CAServer.Telegram;


public interface  ITelegramAuthService
{
    Task<Tuple<string, string>> GetTelegramAuthResultAsync(string param);
    
    Task<TelegramBotDto> GetTelegramBotInfoAsync();

    Task<string> ValidateTelegramHashAndGenerateTokenAsync(TelegramAuthReceiveRequest telegramAuthReceiveRequest);
}