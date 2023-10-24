using System;
using JetBrains.Annotations;

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

    public static string DefaultIfEmpty([CanBeNull] this string source, string defaultVal)
    {
        return source.IsNullOrEmpty() ? defaultVal : source;
    }

    public static bool NotNullOrEmpty([CanBeNull] this string source)
    {
        return !source.IsNullOrEmpty();
    }
    
}