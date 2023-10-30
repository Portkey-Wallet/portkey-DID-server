using System;
using System.Collections.Generic;
using CAServer.Common;
using CodingSeb.ExpressionEvaluator;

namespace CAServer.Commons;

public class ExpressionHelper
{
    
    // InList
    private static readonly Func<object, List<object>, bool> InListFunction = (item, list) =>
        !list.IsNullOrEmpty() && item != null && list.Contains(item);

    private static readonly Dictionary<string, object> ExtensionFunctions = new ()
    {
        ["InList"] = InListFunction
    };

    public static T Evaluate<T>(IEnumerable<string> multilineExpression, Dictionary<string, object> variables = null)
    {
        return Evaluate<T>(string.Join("", multilineExpression), variables);
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