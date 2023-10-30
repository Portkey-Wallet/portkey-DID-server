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
    
}