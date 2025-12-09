using System.Collections.Generic;

namespace CAServer.Telegram.Options;

public class TelegramAuthOptions
{
    public Dictionary<string, string> RedirectUrl { get; set; }
    public string BotId { get; set; }
    public string BotName { get; set; }
    public string BaseUrl { get; set; }
    public int Timeout { get; set; }
}