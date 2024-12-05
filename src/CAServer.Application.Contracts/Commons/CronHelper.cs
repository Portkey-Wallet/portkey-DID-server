using System;
using System.Text;

namespace CAServer.Commons;

public static class CronHelper
{
    public static string GetCronExpression(int seconds)
    {
        if (seconds is <= 0 or >= 36000)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), "Invalid seconds");
        }

        var hours = (seconds / 3600) % 24;
        var minutes = (seconds % 3600) / 60;
        var secondNum = seconds % 60;
        var builder = new StringBuilder();
        builder.Append(secondNum > 0 ? $"*/{secondNum} " : "* ");
        builder.Append(minutes > 0 ? $"*/{minutes} " : "* ");
        builder.Append(hours > 0 ? $"*/{hours} " : "* ");
        builder.Append("* * ?");

        return builder.ToString();
    }
}