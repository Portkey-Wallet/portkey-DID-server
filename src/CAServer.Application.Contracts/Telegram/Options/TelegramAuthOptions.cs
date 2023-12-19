using System.Collections.Generic;

namespace CAServer.Telegram.Options;

public class TelegramAuthOptions
{
    public Dictionary<string, TelegramBtoOptions> Bots { get; set; }
    public string RedirectUrl { get; set; }
    public string DefaultUsed { get; set; }
    public int Expire { get; set; }
}

public class TelegramBtoOptions
{
    public string BotName { get; set; }
    public string Token { get; set; }
}