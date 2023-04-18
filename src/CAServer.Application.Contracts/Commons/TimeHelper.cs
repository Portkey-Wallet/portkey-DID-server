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
    
    
}