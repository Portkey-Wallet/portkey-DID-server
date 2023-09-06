using System;
using JetBrains.Annotations;
using Microsoft.IdentityModel.Tokens;

namespace CAServer.Commons;

public static class StringHelper
{
    
    public static string DefaultIfEmpty([CanBeNull] this string source, string defaultVal)
    {
        return source.IsNullOrEmpty() ? defaultVal : source;
    }

    public static bool NotNullOrEmpty([CanBeNull] this string source)
    {
        return !source.IsNullOrEmpty();
    }
    
}