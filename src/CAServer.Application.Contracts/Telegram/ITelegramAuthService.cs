using System.Threading.Tasks;
using CAServer.Telegram.Dtos;

namespace CAServer.Telegram;


public interface  ITelegramAuthService
{
    Task<TelegramBotDto> GetTelegramBotInfoAsync();

    Task<string> ValidateTelegramHashAndGenerateTokenAsync(TelegramAuthReceiveRequest request);
}