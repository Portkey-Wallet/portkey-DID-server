using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CAServer.Common;
using CodingSeb.ExpressionEvaluator;
using Microsoft.IdentityModel.Tokens;

namespace CAServer.Commons;

public static class ExpressionHelper
{
    
    // InList
    private static readonly Func<object, object, bool> InListFunction = (item, list) =>
        list is IEnumerable enumerable && enumerable.OfType<object>().Any(e => e.Equals(item));

    private static readonly Func<string, string, string, bool> VersionInRangeFunction = (version, from, to) =>
    {
        var current = ClientVersion.Of(version);
        if (current == null) return false;
        
        var fromVer = ClientVersion.Of(from);
        var toVer = ClientVersion.Of(to);
        var bottom = fromVer == null || current > fromVer || current == fromVer;
        var top = toVer == null || current < toVer || current == toVer;
        return bottom && top;
    };


    private static readonly Dictionary<string, object> ExtensionFunctions = new()
    {
        ["InList"] = InListFunction,
        ["VersionInRange"] = VersionInRangeFunction,
    };

    public static bool Evaluate(string expression, Dictionary<string, object> variables = null)
    {
        try
        {
            return Evaluate<bool>(expression, variables);
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    public static bool Evaluate(IEnumerable<string> multilineExpression, Dictionary<string, object> variables = null)
    {
        return Evaluate(string.Join("", multilineExpression), variables);
    }

    public static T Evaluate<T>(string expression, Dictionary<string, object> variables = null)
    {
        AssertHelper.NotEmpty(expression, "Expression cannot be null or whitespace.");

        var evaluator = new ExpressionEvaluator();
        foreach (var (name, function) in ExtensionFunctions)
        {
            evaluator.Variables[name] = function;
        }
        if (variables == null)
        {
            return evaluator.Evaluate<T>(expression);
        }

        foreach (var pair in variables)
        {
            evaluator.Variables[pair.Key] = pair.Value;
        }

        return evaluator.Evaluate<T>(expression);
    }
}