using CAServer;
using CAServer.IpInfo;
using Microsoft.Extensions.Options;
using Xunit;

namespace CAServer.HttpApi.Tests;

public class RealIpOptionsValidatorTests
{
    [Fact]
    public void Validate_Should_Fail_When_HeaderKey_Is_Blank()
    {
        var validator = new RealIpOptionsValidator();

        var result = validator.Validate(string.Empty, new RealIpOptions
        {
            HeaderKey = " "
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, x => x.Contains("RealIp:HeaderKey"));
    }

    [Fact]
    public void Validate_Should_Succeed_When_HeaderKey_Is_Configured()
    {
        var validator = new RealIpOptionsValidator();

        var result = validator.Validate(string.Empty, new RealIpOptions
        {
            HeaderKey = ClientIpHeaders.XForwardedFor
        });

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void Validate_Should_Fail_When_HeaderKey_Has_Leading_Or_Trailing_Whitespace()
    {
        var validator = new RealIpOptionsValidator();

        var result = validator.Validate(string.Empty, new RealIpOptions
        {
            HeaderKey = " X-Forwarded-For "
        });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, x => x.Contains("leading or trailing whitespace"));
    }
}
