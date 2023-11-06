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
        _testOutputHelper.WriteLine(JsonConvert.SerializeObject(ExpressionHelper.Evaluate<List<object>>("List(1,2,'3')")));
        _testOutputHelper.WriteLine(JsonConvert.SerializeObject(ExpressionHelper.Evaluate<bool>("InList(deviceId, List(\"aaa\", \"bbb\", \"3\"))", new Dictionary<string, object>
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
            ["dataList"] = new List<string>{"abc", "123"}
        };  
        
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate<bool>("InList(item, dataList)", param).ToString());
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate<bool>("InList(item, List(\"ab\", \"bc\"))", param).ToString());
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate<bool>("InList(item, List(\"abc\", \"bc\"))", param).ToString());

    }

    [Fact]
    public void StringTest()
    {
        _testOutputHelper.WriteLine(ExpressionHelper.Evaluate<bool>("\"abx\".Contains(\"ab\")").ToString());

    }

}