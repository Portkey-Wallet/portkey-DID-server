namespace CAServer.Monitor.Commons;

public static class MonitorTimeHelper
{
    public static DateTime GetDateTimeFromTimeStamp(long timeStamp)
    {
        var start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return start.AddMilliseconds(timeStamp).ToUniversalTime();
    }
    
    public static long GetTimeStampInMilliseconds()
    {
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }
    
    public static long GetTimeStampInSeconds()
    {
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
    }
    
    public static long GetTimeStampFromDateTime(DateTime dateTime)
    {
        var timeSpan = dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (long)timeSpan.TotalMilliseconds;
    }
}