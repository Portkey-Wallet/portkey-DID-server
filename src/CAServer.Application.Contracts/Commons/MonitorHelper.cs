namespace CAServer.Commons;

public static class MonitorHelper
{
    public static string GetHttpTarget(MonitorRequestType requestType, string url) =>
        $"[{requestType.ToString()}]{url}";
}