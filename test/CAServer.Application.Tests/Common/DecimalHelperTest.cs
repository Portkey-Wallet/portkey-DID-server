using CAServer.Commons;
using Xunit;
using Xunit.Abstractions;

namespace CAServer.Common;

public class DecimalHelperTest
{
    
    private readonly ITestOutputHelper _testOutputHelper;

    public DecimalHelperTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    private static decimal Decimal(decimal d)
    {
        return d;
    }

    
    [Fact]
    public void Test()
    {
        _testOutputHelper.WriteLine(Decimal(200).ToString(2));
        _testOutputHelper.WriteLine(Decimal((decimal)200.000).ToString(2));
        _testOutputHelper.WriteLine(Decimal((decimal)200.001).ToString(2));
        _testOutputHelper.WriteLine(Decimal((decimal)200.009).ToString(2));
        _testOutputHelper.WriteLine(Decimal((decimal)200.009).ToString(2, DecimalHelper.RoundingOption.Floor));
        _testOutputHelper.WriteLine(Decimal((decimal)0).ToString(2));
        _testOutputHelper.WriteLine(Decimal((decimal)0.00001).ToString(2));
    }


}