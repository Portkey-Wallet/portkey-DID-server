namespace CAServer.Monitor;

public class IndicatorOptions
{
    public bool IsEnabled { get; set; }
    public string Application { get; set; } = "PortKey";
    public string Module { get; set; } = "Api";
}