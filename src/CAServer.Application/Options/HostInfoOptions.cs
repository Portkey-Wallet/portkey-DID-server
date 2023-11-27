using CAServer.DataReporting;

namespace CAServer.Options;

public class HostInfoOptions
{
    public NetworkType Network { get; set; } = NetworkType.MainNet;
    public Environment Environment { get; set; } = Environment.Production;
}

public enum Environment
{
    Development,
    Staging,
    Production
}