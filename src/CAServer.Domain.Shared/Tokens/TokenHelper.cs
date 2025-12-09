using System.Text.RegularExpressions;

namespace CAServer.Tokens;

public class TokenHelper
{
    public static TokenType GetTokenType(string symbol)
    {
        int num = Regex.Matches(symbol, "-").Count;
        if (num == 1)
        {
            string[] arr = symbol.Split("-");
            long.TryParse(arr[1], out long itemId);
            if (itemId > 0)
            {
                return TokenType.NFTItem;
            }
            else
            {
                return TokenType.NFTCollection;
            }
        }
        else
        {
            return TokenType.Token;
        }
    }
}