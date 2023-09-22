using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp;

namespace CAServer.Common;

public static class AssertHelper
{
    private const int DefaultErrorCode = 50000;
    private const string DefaultErrorReason = "Assert failed";

    
    public static void IsTrue(bool expression, [CanBeNull] string reason, [ItemCanBeNull] params object[] args)
    {
        IsTrue(expression, DefaultErrorCode, reason, args);
    }


    public static void NotEmpty([CanBeNull] string str, [CanBeNull] string reason, [ItemCanBeNull] params object[] args)
    {
        IsTrue(!str.IsNullOrEmpty(), reason, args);
    }
    
    public static void NotEmpty(Guid guid, [CanBeNull] string reason, [ItemCanBeNull] params object[] args)
    {
        IsTrue(guid != Guid.Empty, reason, args);
    }
    
    public static void NotEmpty<T>([CanBeNull] IEnumerable<T> collection, [CanBeNull] string reason, [ItemCanBeNull] params object[] args)
    {
        IsTrue(!collection.IsNullOrEmpty(), reason, args);
    }

    public static void NullOrEmpty([CanBeNull] string str, [CanBeNull] string reason, [ItemCanBeNull] params object[] args)
    {
        IsTrue(str.IsNullOrEmpty(), reason, args);
    }
    
    public static void NullOrEmpty(Guid guid, [CanBeNull] string reason, [ItemCanBeNull] params object[] args)
    {
        IsTrue(guid == Guid.Empty, reason, args);
    }
    
    public static void NullOrEmpty<T>([CanBeNull] IEnumerable<T> collection, [CanBeNull] string reason, [ItemCanBeNull] params object[] args)
    {
        IsTrue(collection.IsNullOrEmpty(), reason, args);
    }

    public static void NotNull(object obj, [CanBeNull] string reason, [ItemCanBeNull] params object[] args)
    {
        IsTrue(obj != null, reason, args);
    }

    public static void IsTrue(bool expression, int code = DefaultErrorCode, [CanBeNull] string reason = DefaultErrorReason, [ItemCanBeNull] params object[] args)
    {
        if (!expression)
        {
            throw new UserFriendlyException(Format(reason, args), code.ToString());
        }
    }
    
    private static string Format(string template, params object[] values)
    {
        if (values == null || values.Length == 0)
            return template;
        
        var valueIndex = 0;
        var start = 0;
        int placeholderStart;
        var result = new StringBuilder();

        while ((placeholderStart = template.IndexOf('{', start)) != -1)
        {
            var placeholderEnd = template.IndexOf('}', placeholderStart);
            if (placeholderEnd == -1) break;

            result.Append(template, start, placeholderStart - start);

            if (valueIndex < values.Length)
                result.Append((values[valueIndex++] ?? "null"));
            else
                result.Append(template, placeholderStart, placeholderEnd - placeholderStart + 1);

            start = placeholderEnd + 1;
        }

        if (start < template.Length)
            result.Append(template, start, template.Length - start);

        return result.ToString();
    }
}