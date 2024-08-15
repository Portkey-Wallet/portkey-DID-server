namespace CAServer.Telegram.Options;

public class TelegramVerifierOptions
{
    public string Url { get; set; }
    public int Timeout { get; set; }
    public int BotIdMinimumLength { get; set; } = 9;
    public int BotIdMaximumLength { get; set; } = 11;
    public int SecretMinimumLength { get; set; } = 45;
    public int SecretMaximumLength { get; set; } = 47;
}