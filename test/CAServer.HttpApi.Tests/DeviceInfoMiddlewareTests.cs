using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.IpInfo;
using CAServer.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CAServer.HttpApi.Tests;

public class DeviceInfoMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Should_Use_XRealIp_When_XForwardedFor_Is_Missing()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[ClientIpHeaders.XRealIp] = "4.4.4.4";
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("5.5.5.5");

        var resolver = TestHttpClientIpResolverFactory.Create(httpContext);
        DeviceInfo capturedDeviceInfo = null;
        var middleware = new DeviceInfoMiddleware(_ =>
        {
            capturedDeviceInfo = DeviceInfoContext.CurrentDeviceInfo;
            return Task.CompletedTask;
        }, resolver, Mock.Of<ILogger<DeviceInfoMiddleware>>());

        await middleware.InvokeAsync(httpContext);

        Assert.NotNull(capturedDeviceInfo);
        Assert.Equal("4.4.4.4", capturedDeviceInfo.ClientIp);
        Assert.Null(DeviceInfoContext.CurrentDeviceInfo);
    }
}
