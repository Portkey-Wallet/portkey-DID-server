namespace CAServer.Nightingale.Logging;

public class N9EClientForLoggingOptions
{
    public bool DisableLogging { get; set; } = false;

    public string LogFilePathFormat { get; set; } = "./Logs/trace-.log";

    public int LogRetainedFileCountLimit { get; set; } = 5;

    public int MetricNameMaxLength { get; set; } = 100;
}