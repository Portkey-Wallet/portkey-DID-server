using System;

namespace CAServer.Common;

public static class TimeStampHelper
{
    public static string GetTimeStampInMilliseconds()
    {
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();
    }
    
    public static string GetTimeStampInSeconds()
    {
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();
    }
}