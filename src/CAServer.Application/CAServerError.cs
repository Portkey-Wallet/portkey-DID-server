using System.Collections.Generic;

namespace CAServer;

public class CAServerError
{
    public const int InvalidInput = 400;
    public static readonly Dictionary<int, string> Message = new()
    {
        { InvalidInput, "Invalid input params." },
    };
}