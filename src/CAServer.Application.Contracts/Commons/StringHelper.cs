using System;
using JetBrains.Annotations;

namespace CAServer.Commons;

public static class StringHelper
{
    
    public static string DefaultIfEmpty([CanBeNull] this string source, string defaultVal)
    {
        return source.IsNullOrEmpty() ? defaultVal : source;
    }
    
}