using System;
using System.Globalization;

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

    public static string ToString(this decimal value, int decimalPlaces, RoundingOption roundingOption = RoundingOption.Round)
    {
        decimal multiplier = 1;
        for (var i = 0; i < decimalPlaces; i++)
        {
            multiplier *= 10;
        }
        var roundedValue = roundingOption switch
        {
            RoundingOption.Round => Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero),
            RoundingOption.Ceiling => Math.Ceiling(value * multiplier) / multiplier,
            RoundingOption.Floor => Math.Floor(value * multiplier) / multiplier,
            RoundingOption.Truncate => Math.Truncate(value * multiplier) / multiplier,
            _ => throw new ArgumentOutOfRangeException(nameof(roundingOption), "Invalid rounding option.")
        };

        var isInteger = roundedValue == Math.Floor(roundedValue);
        var formatString = isInteger
            ? "0"
            : $"0.{new string('#', decimalPlaces)}";
        return roundedValue.ToString(formatString, CultureInfo.InvariantCulture);
    }


}