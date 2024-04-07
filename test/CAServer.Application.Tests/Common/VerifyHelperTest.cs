using CAServer.Commons;
using Shouldly;
using Xunit;

namespace CAServer.Common;

public class VerifyHelperTest
{


    [Fact]
    public void TestErrorMessage()
    {
        var dic = CAServerError.Message;
        dic[400].ShouldBe("Invalid input params.");
    }
}