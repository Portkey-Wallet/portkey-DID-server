using System;
using System.Text;
using JetBrains.Annotations;
using Volo.Abp;

namespace CAServer.Common;

public static class AssertHelper
{
    private const string DefaultErrorCode = "50000";

    
    public static void IsTrue(bool expression, string reason, params string[] args)
    {
        IsTrue(expression, DefaultErrorCode, reason, args);
    }


    public static void NotEmpty([CanBeNull] string str, string reason, params string[] args)
    {
        IsTrue(!str.IsNullOrEmpty(), reason, args);
    }

    public static void NotNull(object obj, string reason, params string[] args)
    {
        IsTrue(obj != null, reason, args);
    }

    public static void IsTrue(bool expression, string code, string reason, params string[] args)
    {
        if (!expression)
        {
            throw new UserFriendlyException(Format(reason, args), code);
        }
    }
    
    private static string Format(string template, params string[] values)
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
                result.Append(values[valueIndex++]);
            else
                result.Append(template, placeholderStart, placeholderEnd - placeholderStart + 1);

            start = placeholderEnd + 1;
        }

        if (start < template.Length)
            result.Append(template, start, template.Length - start);

        return result.ToString();
    }
}