using System.Threading.Tasks;

namespace CAServer.Telegram;

public interface ITelegramRateLimiter
{
    Task RecordRequestAsync();
}