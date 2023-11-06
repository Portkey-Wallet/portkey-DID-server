using System;

namespace CAServer.Commons;

public static class TimeHelper
{
    public static DateTime GetDateTimeFromTimeStamp(long timeStamp)
    {
        DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        //return start.AddMilliseconds(timeStamp).ToLocalTime(); // DateTime.UtcNow.
        return start.AddMilliseconds(timeStamp).ToUniversalTime();
    }
    
    public static DateTime GetDateTimeFromSecondTimeStamp(long timeStamp)
    {
        DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        //return start.AddMilliseconds(timeStamp).ToLocalTime(); // DateTime.UtcNow.
        return start.AddSeconds(timeStamp).ToUniversalTime();
    }
    
    public static long GetTimeStampInMilliseconds()
    {
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }
    
    public static long GetTimeStampInSeconds()
    {
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
    }
}