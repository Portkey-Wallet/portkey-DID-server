using System;

namespace CAServer.Commons;

public static class DateTimeHelper
{


    public static string ToUtcString(this DateTime dateTime)
    {
        return dateTime.ToString("o");
    }
     
}