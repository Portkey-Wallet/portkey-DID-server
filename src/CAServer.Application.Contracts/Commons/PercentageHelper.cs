using System;

namespace CAServer.Commons;

public static class PercentageHelper
{
    public static string CalculatePercentage(int numerator, int denominator)
    {
        var percentage = (double)numerator / denominator * 100;
        var formatted = Math.Truncate(percentage * 100) / 100;
        return formatted.ToString() + "%";
    }
}