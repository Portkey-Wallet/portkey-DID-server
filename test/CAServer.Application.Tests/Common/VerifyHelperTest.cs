using CAServer.Commons;
using Shouldly;
using Xunit;

namespace CAServer.Common;

public class VerifyHelperTest
{
    [Fact]
    public void TestPhone()
    {
        var result = VerifyHelper.VerifyPhone("12345678901");
        result.ShouldBeTrue();
    }
}