using System.Collections.Generic;
using System.Text.RegularExpressions;
using CAServer.Options;
using CAServer.Signature;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace CAServer.AppleAuth;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class AppleAuthTransferTest : CAServerApplicationTestBase
{
    private readonly AppleTransferOptions _appleTransferOptions;

    public AppleAuthTransferTest()
    {
        _appleTransferOptions = MockAppleTransferOptions().Value;
    }

    [Fact]
    public void IsNeedInterceptTest()
    {
        var email = "test@qq.com";
        var userId = "000995.3e3e081a64284904b8ce2d0296dcbbfd.0300";
        _appleTransferOptions.IsNeedIntercept(userId).ShouldBe(true);
        _appleTransferOptions.IsNeedIntercept(email).ShouldBe(false);

        _appleTransferOptions.WhiteList.Add(userId);
        _appleTransferOptions.IsNeedIntercept(userId).ShouldBe(false);
        _appleTransferOptions.IsNeedIntercept(email).ShouldBe(false);

        _appleTransferOptions.CloseLogin = false;
        _appleTransferOptions.IsNeedIntercept(userId).ShouldBe(false);
        _appleTransferOptions.IsNeedIntercept(email).ShouldBe(false);
    }

    private IOptionsSnapshot<AppleTransferOptions> MockAppleTransferOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<AppleTransferOptions>>();
        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new AppleTransferOptions
            {
                CloseLogin = true,
                ErrorMessage = "test",
                WhiteList = new List<string> { "" }
            });
        return mockOptionsSnapshot.Object;
    }
}