using System;

namespace CAServer.Commons;

public static class PercentageHelper
{
    public static string CalculatePercentage(int numerator, int denominator)
    {
        double percentage = Math.Round((double)numerator / denominator * 100, 4);
        return percentage.ToString("F2") + "%";
    }
}