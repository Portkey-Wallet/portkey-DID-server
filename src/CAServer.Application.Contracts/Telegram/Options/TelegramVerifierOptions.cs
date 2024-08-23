namespace CAServer.Telegram.Options;

public class TelegramVerifierOptions
{
    public string Url { get; set; }
    public int Timeout { get; set; }
    public int BotIdMinimumLength { get; set; } = 9;
    public int BotIdMaximumLength { get; set; } = 11;
    public int SecretMinimumLength { get; set; } = 34;
    public int SecretMaximumLength { get; set; } = 36;

    public int ReplenishmentPeriodSeconds { get; set; } = 60;
    public int TokenLimit { get; set; } = 5;
    public int TokensPerPeriod { get; set; } = 5;
}