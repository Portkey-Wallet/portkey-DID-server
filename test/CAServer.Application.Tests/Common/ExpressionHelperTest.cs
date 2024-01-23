using System.Collections.Generic;
using CAServer.Commons;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace CAServer.Common;

public class ExpressionHelperTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ExpressionHelperTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void EvaluateTest()
    {
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate<int>("1 + 2").ToString());
        _testOutputHelper.WriteLine(
            JsonConvert.SerializeObject(ExpressionHelper.Evaluate<List<object>>("List(1,2,'3')")));
        _testOutputHelper.WriteLine(JsonConvert.SerializeObject(ExpressionHelper.Evaluate(
            "InList(deviceId, List(\"aaa\", \"bbb\", \"3\"))", new Dictionary<string, object>
            {
                ["deviceId"] = "aaa"
            })));
    }

    [Fact]
    public void InListTest()
    {
        var param = new Dictionary<string, object>
        {
            ["item"] = "abc",
            ["dataList"] = new List<string> { "abc", "123" }
        };

        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate("InList(item, dataList)", param).ToString());
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate("InList(item, List(\"ab\", \"bc\"))", param).ToString());
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate("InList(item, List(\"abc\", \"bc\"))", param).ToString());
    }

    [Fact]
    public void StringTest()
    {
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate("\"abx\".Contains(\"ab\")").ToString());
    }

    [Fact]
    public void ParamTest()
    {
        var param = new Dictionary<string, object>()
        {
            ["currency"] = "USD"
        };

        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate("""  param["currency"] == "USD" """,
            new Dictionary<string, object>()
            {
                ["param"] = param
            }).ToString());

        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate("""  param["currencySymbol"] == "USD" """,
            new Dictionary<string, object>()
            {
                ["param"] = param
            }).ToString());
    }

    [Fact]
    public void VersionInRangeTest()
    {
        var param = new Dictionary<string, object>()
        {
            ["version"] = "1.2.0"
        };
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate("VersionInRange(version, \"1.2.0\", \"1.2.3\")", param).ToString());
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate("VersionInRange(version, \"1.2.1\", \"1.2.3\")", param).ToString());
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate("VersionInRange(version, \"1.1.999999\", \"1.2.0\")", param).ToString());
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate("VersionInRange(version, \"1.1.999998\", \"1.1.999999\")", param).ToString());
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate("VersionInRange(version, \"1.1.999999\", null)", param).ToString());
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate("VersionInRange(version, null, \"1.2.0\")", param).ToString());
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate("VersionInRange(version, null, null)", param).ToString());
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate("VersionInRange(version, \"1.1.999999\")", param).ToString());
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate("VersionInRange(version, null, \"1.2.0\")", param).ToString());
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate("VersionInRange(version, null, null)", param).ToString());
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate("VersionInRange(version, \"1.2.0\", null)", param).ToString());
    }
}