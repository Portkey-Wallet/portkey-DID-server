using System;

namespace CAServer.Commons;

public static class DecimalHelper
{
    public enum RoundingOption
    {
        Round,
        Ceiling,
        Floor,
        Truncate
    }

    public static string ToString(this decimal value, int decimalPlaces, RoundingOption roundingOption)
    {
        return roundingOption switch
        {
            RoundingOption.Round => Math.Round(value, decimalPlaces).ToString($"N{decimalPlaces}"),
            RoundingOption.Ceiling => (Math.Ceiling(value * (decimal)Math.Pow(10, decimalPlaces)) /
                                       (decimal)Math.Pow(10, decimalPlaces)).ToString($"N{decimalPlaces}"),
            RoundingOption.Floor => (Math.Floor(value * (decimal)Math.Pow(10, decimalPlaces)) /
                                     (decimal)Math.Pow(10, decimalPlaces)).ToString($"N{decimalPlaces}"),
            RoundingOption.Truncate => (Math.Truncate(value * (decimal)Math.Pow(10, decimalPlaces)) /
                                        (decimal)Math.Pow(10, decimalPlaces)).ToString($"N{decimalPlaces}"),
            _ => throw new ArgumentOutOfRangeException(nameof(roundingOption), "Invalid rounding option.")
        };
    }
}