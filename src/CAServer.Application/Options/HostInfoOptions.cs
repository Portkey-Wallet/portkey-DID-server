namespace CAServer.Options;

public class HostInfoOptions
{
    public Environment Environment { get; set; } = Environment.Production;
}

public enum Environment
{
    Development,
    Staging,
    Production
}