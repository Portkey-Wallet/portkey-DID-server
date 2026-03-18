using System.Net;
using CAServer;
using CAServer.IpInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CAServer.IpInfo;

public class HttpClientIpResolverTests
{
    [Fact]
    public void GetForwardedClientIp_Should_Use_First_XForwardedFor()
    {
        var resolver = CreateResolver();
        resolver.HttpContext.Request.Headers[ClientIpHeaders.XForwardedFor] = "1.1.1.1, 2.2.2.2";
        resolver.HttpContext.Request.Headers[ClientIpHeaders.XRealIp] = "3.3.3.3";

        Assert.Equal("1.1.1.1", resolver.Service.GetForwardedClientIp());
    }

    [Fact]
    public void GetForwardedClientIp_Should_Fallback_To_XRealIp()
    {
        var resolver = CreateResolver();
        resolver.HttpContext.Request.Headers[ClientIpHeaders.XRealIp] = "4.4.4.4";

        Assert.Equal("4.4.4.4", resolver.Service.GetForwardedClientIp());
    }

    [Fact]
    public void GetForwardedClientIp_Should_Return_Null_When_Headers_Are_Missing()
    {
        var resolver = CreateResolver(remoteIpAddress: "5.5.5.5");

        Assert.Null(resolver.Service.GetForwardedClientIp());
    }

    [Fact]
    public void GetForwardedClientIp_Should_Prioritize_Configured_HeaderKey()
    {
        var resolver = CreateResolver(configuredHeaderKey: "X-Client-IP");
        resolver.HttpContext.Request.Headers["X-Client-IP"] = "20.20.20.20";
        resolver.HttpContext.Request.Headers[ClientIpHeaders.XForwardedFor] = "21.21.21.21";
        resolver.HttpContext.Request.Headers[ClientIpHeaders.XRealIp] = "22.22.22.22";

        Assert.Equal("20.20.20.20", resolver.Service.GetForwardedClientIp());
    }

    [Fact]
    public void GetBestEffortClientIp_Should_Prefer_RequestScoped_Resolved_Ip()
    {
        var resolver = CreateResolver(remoteIpAddress: "6.6.6.6");
        resolver.Service.SetResolvedClientIp("7.7.7.7");

        Assert.Equal("7.7.7.7", resolver.Service.GetBestEffortClientIp());
    }

    [Fact]
    public void GetBestEffortClientIp_Should_Fallback_To_XForwardedFor_XRealIp_Then_RemoteIp()
    {
        var withHeader = CreateResolver(remoteIpAddress: "8.8.8.8");
        withHeader.HttpContext.Request.Headers[ClientIpHeaders.XForwardedFor] = "9.9.9.9, 10.10.10.10";
        Assert.Equal("9.9.9.9", withHeader.Service.GetBestEffortClientIp());

        var withXRealIp = CreateResolver(remoteIpAddress: "10.10.10.10");
        withXRealIp.HttpContext.Request.Headers[ClientIpHeaders.XRealIp] = "10.10.10.11";
        Assert.Equal("10.10.10.11", withXRealIp.Service.GetBestEffortClientIp());

        var withRemoteIp = CreateResolver(remoteIpAddress: "11.11.11.11");
        Assert.Equal("11.11.11.11", withRemoteIp.Service.GetBestEffortClientIp());
    }

    [Fact]
    public void GetBestEffortClientIp_Should_Prioritize_Configured_HeaderKey()
    {
        var resolver = CreateResolver(remoteIpAddress: "23.23.23.23", configuredHeaderKey: "X-Client-IP");
        resolver.HttpContext.Request.Headers["X-Client-IP"] = "24.24.24.24";
        resolver.HttpContext.Request.Headers[ClientIpHeaders.XForwardedFor] = "25.25.25.25";

        Assert.Equal("24.24.24.24", resolver.Service.GetBestEffortClientIp());
    }

    [Fact]
    public void GetFirstHeaderIp_Should_Support_Configured_Header_Name()
    {
        var resolver = CreateResolver();
        resolver.HttpContext.Request.Headers["Custom-Header"] = "12.12.12.12, 13.13.13.13";

        Assert.Equal("12.12.12.12", resolver.Service.GetFirstHeaderIp("Custom-Header"));
    }

    private static (HttpClientIpResolver Service, DefaultHttpContext HttpContext) CreateResolver(
        string remoteIpAddress = null, string configuredHeaderKey = ClientIpHeaders.XForwardedFor)
    {
        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrWhiteSpace(remoteIpAddress))
        {
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(remoteIpAddress);
        }

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(httpContext);
        return (new HttpClientIpResolver(accessor.Object, Microsoft.Extensions.Options.Options.Create(new RealIpOptions
        {
            HeaderKey = configuredHeaderKey
        })), httpContext);
    }
}
