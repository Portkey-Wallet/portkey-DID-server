using System;

namespace CAServer.Commons;

public static class StringHelper
{
    public static string RemovePrefix(string input, string prefix)
    {
        if (input.IsNullOrEmpty() || prefix.IsNullOrEmpty())
        {
            return "";
        }
        
        return input.StartsWith(prefix) ? input[prefix.Length..] : input;
    }
    
}