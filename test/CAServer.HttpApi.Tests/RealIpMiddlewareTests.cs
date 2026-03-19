using System;
using System.Threading.Tasks;
using CAServer.IpInfo;
using CAServer.IpWhiteList;
using CAServer.IpWhiteList.Dtos;
using CAServer.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.HttpApi.Tests;

public class RealIpMiddlewareTests
{
    [Fact]
    public async Task Invoke_Should_Use_Legacy_Fallback_Header_When_Configured_Header_Is_Missing()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/app/account/sendVerificationRequest";
        httpContext.Request.Headers[ClientIpHeaders.XForwardedFor] = "8.8.8.8";
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.Id).Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var ipWhiteListAppService = new Mock<IIpWhiteListAppService>();
        var resolver = TestHttpClientIpResolverFactory.Create(httpContext, "X-Client-IP", true);

        var middleware = new RealIpMiddleware(_ => Task.CompletedTask, Mock.Of<ILogger<RealIpMiddleware>>(),
            ipWhiteListAppService.Object,
            Microsoft.Extensions.Options.Options.Create(new AddToWhiteListUrlsOptions
            {
                Urls = new() { "/api/app/account/sendVerificationRequest" }
            }),
            currentUser.Object, resolver);

        await middleware.Invoke(httpContext);

        ipWhiteListAppService.Verify(x => x.AddIpWhiteListAsync(It.Is<AddUserIpToWhiteListRequestDto>(dto =>
            dto.UserIp == "8.8.8.8" &&
            dto.UserId == Guid.Parse("11111111-1111-1111-1111-111111111111"))), Times.Once);
    }

    [Fact]
    public async Task Invoke_Should_Not_Use_Legacy_Fallback_Header_When_Disabled()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/app/account/sendVerificationRequest";
        httpContext.Request.Headers[ClientIpHeaders.XForwardedFor] = "8.8.8.8";
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.Id).Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var ipWhiteListAppService = new Mock<IIpWhiteListAppService>();
        var resolver = TestHttpClientIpResolverFactory.Create(httpContext, "X-Client-IP", false);

        var middleware = new RealIpMiddleware(_ => Task.CompletedTask, Mock.Of<ILogger<RealIpMiddleware>>(),
            ipWhiteListAppService.Object,
            Microsoft.Extensions.Options.Options.Create(new AddToWhiteListUrlsOptions
            {
                Urls = new() { "/api/app/account/sendVerificationRequest" }
            }),
            currentUser.Object, resolver);

        await middleware.Invoke(httpContext);

        ipWhiteListAppService.Verify(x => x.AddIpWhiteListAsync(It.IsAny<AddUserIpToWhiteListRequestDto>()),
            Times.Never);
    }
}
