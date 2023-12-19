using System.Collections.Generic;

namespace CAServer.Telegram.Options;

public class TelegramAuthOptions
{
    public Dictionary<string, TelegramBtoOptions> Bots { get; set; }
    public Dictionary<string, string> RedirectUrl { get; set; }
    public string DefaultUsed { get; set; }
    public int Expire { get; set; }
}

public class TelegramBtoOptions
{
    public string BotId { get; set; }
    public string BotName { get; set; }
    public string Token { get; set; }
}