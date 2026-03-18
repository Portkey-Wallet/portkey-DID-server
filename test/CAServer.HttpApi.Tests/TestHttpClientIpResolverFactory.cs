using CAServer.IpInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace CAServer.HttpApi.Tests;

internal static class TestHttpClientIpResolverFactory
{
    public static IHttpClientIpResolver Create(DefaultHttpContext httpContext,
        string configuredHeaderKey = ClientIpHeaders.XForwardedFor, bool allowLegacyForwardedFallback = true)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(httpContext);
        return new HttpClientIpResolver(accessor.Object, Microsoft.Extensions.Options.Options.Create(new RealIpOptions
        {
            HeaderKey = configuredHeaderKey,
            AllowLegacyForwardedFallback = allowLegacyForwardedFallback
        }));
    }
}
