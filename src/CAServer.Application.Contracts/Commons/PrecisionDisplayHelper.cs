using System.Globalization;

namespace CAServer.Commons;

public class PrecisionDisplayHelper
{
    
    public static string FormatNumber(string input)
    {
        if (decimal.TryParse(input, out decimal result))
        {
            return FormatNumber(result);
        }
        else
        {
            return input;
        }
    }
    
    public static string FormatNumber(decimal number)
    {
        if (number >= 0.01m)
        {
            return number.ToString("0.##", CultureInfo.InvariantCulture);
        }
        else
        {
            return number.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }
}