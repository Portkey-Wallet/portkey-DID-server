using System;
using System.Net;
using System.Threading.Tasks;
using CAServer.CAAccount;
using CAServer.Dtos;
using CAServer.Google;
using CAServer.Google.Dtos;
using CAServer.IpInfo;
using CAServer.IpWhiteList;
using CAServer.Switch;
using CAServer.Switch.Dtos;
using CAServer.Verifier;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.HttpApi.Tests;

public class VerificationRequestOperationHandlerTests
{
    [Fact]
    public async Task CreateCaHolderHandler_Should_Return_400_When_Strict_Ip_Is_Missing()
    {
        var setup = CreateHandlerSetup();
        setup.RateLimitService.Setup(x => x.GetPolicy(It.IsAny<RegistrationEmailRateLimitContext>()))
            .Returns(new RegistrationEmailRateLimitPolicy
            {
                GuardianType = "Email",
                OperationType = OperationType.CreateCAHolder,
                Per10Minutes = 10,
                PerHour = 30
            });

        var handler = new CreateCaHolderVerificationRequestHandler(setup.ClientIpResolver,
            Mock.Of<ILogger<CreateCaHolderVerificationRequestHandler>>(), setup.RateLimitService.Object,
            setup.VerifierAppService.Object);

        var result = await handler.HandleAsync(CreateContext(OperationType.CreateCAHolder));

        Assert.True(result.IsHandled);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        setup.VerifierAppService.Verify(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateCaHolderHandler_Should_Direct_Send_When_RateLimit_Policy_Does_Not_Apply()
    {
        var setup = CreateHandlerSetup();
        setup.VerifierAppService.Setup(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()))
            .ReturnsAsync(new VerifierServerResponse
            {
                VerifierSessionId = Guid.NewGuid()
            });
        setup.RateLimitService.Setup(x => x.GetPolicy(It.IsAny<RegistrationEmailRateLimitContext>()))
            .Returns((RegistrationEmailRateLimitPolicy)null);

        var handler = new CreateCaHolderVerificationRequestHandler(setup.ClientIpResolver,
            Mock.Of<ILogger<CreateCaHolderVerificationRequestHandler>>(), setup.RateLimitService.Object,
            setup.VerifierAppService.Object);

        var result = await handler.HandleAsync(CreateContext(OperationType.CreateCAHolder, guardianType: "Phone"));

        Assert.True(result.IsHandled);
        Assert.NotNull(result.Response);
        setup.RateLimitService.Verify(x => x.CheckAsync(It.IsAny<RegistrationEmailRateLimitContext>()), Times.Never);
        setup.VerifierAppService.Verify(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()),
            Times.Once);
    }

    [Fact]
    public async Task SocialRecoveryHandler_Should_Preserve_Baseline_When_Feature_Is_Disabled_And_CheckSwitch_Is_Off()
    {
        var setup = CreateHandlerSetup(checkSwitchOpen: false);
        setup.VerifierAppService.Setup(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()))
            .ReturnsAsync(new VerifierServerResponse
            {
                VerifierSessionId = Guid.NewGuid()
            });
        setup.VerifierAppService.Setup(x => x.GuardianExistsAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("should not be called"));
        setup.RateLimitService.Setup(x => x.GetPolicy(It.IsAny<RegistrationEmailRateLimitContext>()))
            .Returns((RegistrationEmailRateLimitPolicy)null);

        var handler = new SocialRecoveryVerificationRequestHandler(setup.ClientIpResolver,
            Mock.Of<ILogger<SocialRecoveryVerificationRequestHandler>>(), setup.RateLimitService.Object,
            setup.RiskControlService.Object, setup.SwitchAppService.Object, setup.VerifierAppService.Object);

        var result = await handler.HandleAsync(CreateContext(OperationType.SocialRecovery));

        Assert.True(result.IsHandled);
        Assert.NotNull(result.Response);
        setup.VerifierAppService.Verify(x => x.GuardianExistsAsync(It.IsAny<string>()), Times.Never);
        setup.VerifierAppService.Verify(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()),
            Times.Once);
    }

    [Fact]
    public async Task SocialRecoveryHandler_Should_Return_Null_When_Guardian_Missing_In_Legacy_Path()
    {
        var setup = CreateHandlerSetup(checkSwitchOpen: true);
        setup.VerifierAppService.Setup(x => x.GuardianExistsAsync("missing@example.com")).ReturnsAsync(false);
        setup.RateLimitService.Setup(x => x.GetPolicy(It.IsAny<RegistrationEmailRateLimitContext>()))
            .Returns((RegistrationEmailRateLimitPolicy)null);

        var handler = new SocialRecoveryVerificationRequestHandler(setup.ClientIpResolver,
            Mock.Of<ILogger<SocialRecoveryVerificationRequestHandler>>(), setup.RateLimitService.Object,
            setup.RiskControlService.Object, setup.SwitchAppService.Object, setup.VerifierAppService.Object);

        var result = await handler.HandleAsync(CreateContext(OperationType.SocialRecovery,
            guardianIdentifier: "missing@example.com"));

        Assert.True(result.IsHandled);
        Assert.Null(result.Response);
        setup.RiskControlService.Verify(x => x.HandleGuardianOperationAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<SendVerificationRequestInput>(), It.IsAny<OperationType>()), Times.Never);
    }

    [Fact]
    public async Task SocialRecoveryHandler_Should_Not_Consume_Quota_When_Guardian_Is_Missing_In_RateLimited_Path()
    {
        var setup = CreateHandlerSetup(checkSwitchOpen: false);
        setup.VerifierAppService.Setup(x => x.GuardianExistsAsync("missing@example.com")).ReturnsAsync(false);
        setup.RateLimitService.Setup(x => x.GetPolicy(It.IsAny<RegistrationEmailRateLimitContext>()))
            .Returns(new RegistrationEmailRateLimitPolicy
            {
                GuardianType = "Email",
                OperationType = OperationType.SocialRecovery,
                Per10Minutes = 15,
                PerHour = 45,
                RequireGuardianExistsBeforeConsume = true
            });

        var handler = new SocialRecoveryVerificationRequestHandler(setup.ClientIpResolver,
            Mock.Of<ILogger<SocialRecoveryVerificationRequestHandler>>(), setup.RateLimitService.Object,
            setup.RiskControlService.Object, setup.SwitchAppService.Object, setup.VerifierAppService.Object);

        var result = await handler.HandleAsync(CreateContext(OperationType.SocialRecovery,
            guardianIdentifier: "missing@example.com"));

        Assert.True(result.IsHandled);
        Assert.Null(result.Response);
        setup.RateLimitService.Verify(x => x.CheckAsync(It.IsAny<RegistrationEmailRateLimitContext>()), Times.Never);
    }

    [Fact]
    public async Task SocialRecoveryHandler_Should_Apply_RateLimit_Then_Direct_Send_When_CheckSwitch_Is_Off()
    {
        var setup = CreateHandlerSetup(checkSwitchOpen: false);
        setup.HttpContext.Request.Headers[ClientIpHeaders.XForwardedFor] = "6.6.6.6";
        setup.VerifierAppService.Setup(x => x.GuardianExistsAsync("user@example.com")).ReturnsAsync(true);
        setup.VerifierAppService.Setup(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()))
            .ReturnsAsync(new VerifierServerResponse
            {
                VerifierSessionId = Guid.NewGuid()
            });
        setup.RateLimitService.Setup(x => x.GetPolicy(It.IsAny<RegistrationEmailRateLimitContext>()))
            .Returns(new RegistrationEmailRateLimitPolicy
            {
                GuardianType = "Email",
                OperationType = OperationType.SocialRecovery,
                Per10Minutes = 15,
                PerHour = 45,
                RequireGuardianExistsBeforeConsume = true
            });
        setup.RateLimitService.Setup(x => x.CheckAsync(It.Is<RegistrationEmailRateLimitContext>(c =>
                c.ClientIp == "6.6.6.6" && c.OperationType == OperationType.SocialRecovery)))
            .ReturnsAsync(RegistrationEmailRateLimitCheckResult.Allow());

        var handler = new SocialRecoveryVerificationRequestHandler(setup.ClientIpResolver,
            Mock.Of<ILogger<SocialRecoveryVerificationRequestHandler>>(), setup.RateLimitService.Object,
            setup.RiskControlService.Object, setup.SwitchAppService.Object, setup.VerifierAppService.Object);

        var result = await handler.HandleAsync(CreateContext(OperationType.SocialRecovery));

        Assert.True(result.IsHandled);
        Assert.NotNull(result.Response);
        setup.RiskControlService.Verify(x => x.HandleGuardianOperationAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<SendVerificationRequestInput>(), It.IsAny<OperationType>()), Times.Never);
        setup.VerifierAppService.Verify(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()),
            Times.Once);
    }

    [Fact]
    public async Task SocialRecoveryHandler_Should_Reuse_Resolved_XRealIp_In_Risk_Control_Path()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[ClientIpHeaders.XRealIp] = "4.4.4.4";
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(httpContext);
        var clientIpResolver = new TestHttpClientIpResolver(accessor.Object);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);

        var verifierAppService = new Mock<IVerifierAppService>();
        verifierAppService.Setup(x => x.GuardianExistsAsync("user@example.com")).ReturnsAsync(true);
        verifierAppService.Setup(x => x.CountVerifyCodeInterfaceRequestAsync("4.4.4.4")).ReturnsAsync(1);
        verifierAppService.Setup(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()))
            .ReturnsAsync(new VerifierServerResponse
            {
                VerifierSessionId = Guid.NewGuid()
            });

        var googleAppService = new Mock<IGoogleAppService>();
        googleAppService.Setup(x => x.IsGoogleRecaptchaOpenAsync("4.4.4.4", OperationType.SocialRecovery))
            .ReturnsAsync(false);

        var ipWhiteListAppService = new Mock<IIpWhiteListAppService>();
        ipWhiteListAppService.Setup(x => x.IsInWhiteListAsync("4.4.4.4")).ReturnsAsync(true);

        var switchAppService = new Mock<ISwitchAppService>();
        switchAppService.Setup(x => x.GetSwitchStatus(It.IsAny<string>())).Returns((string switchName) => new SwitchDto
        {
            IsOpen = switchName switch
            {
                "CheckSwitch" => true,
                "GoogleRecaptcha" => false,
                _ => false
            }
        });

        var rateLimitService = new Mock<IRegistrationEmailRateLimitService>();
        rateLimitService.Setup(x => x.GetPolicy(It.IsAny<RegistrationEmailRateLimitContext>()))
            .Returns(new RegistrationEmailRateLimitPolicy
            {
                GuardianType = "Email",
                OperationType = OperationType.SocialRecovery,
                Per10Minutes = 15,
                PerHour = 45,
                RequireGuardianExistsBeforeConsume = true
            });
        rateLimitService.Setup(x => x.CheckAsync(It.Is<RegistrationEmailRateLimitContext>(c =>
                c.ClientIp == "4.4.4.4" && c.OperationType == OperationType.SocialRecovery)))
            .ReturnsAsync(RegistrationEmailRateLimitCheckResult.Allow());

        var secondaryEmailAppService = new Mock<ISecondaryEmailAppService>();
        var riskControlService = new VerificationRequestRiskControlService(currentUser.Object, googleAppService.Object,
            ipWhiteListAppService.Object, clientIpResolver,
            Mock.Of<ILogger<VerificationRequestRiskControlService>>(), secondaryEmailAppService.Object,
            switchAppService.Object, verifierAppService.Object);
        var handler = new SocialRecoveryVerificationRequestHandler(clientIpResolver,
            Mock.Of<ILogger<SocialRecoveryVerificationRequestHandler>>(), rateLimitService.Object, riskControlService,
            switchAppService.Object, verifierAppService.Object);

        var result = await handler.HandleAsync(CreateContext(OperationType.SocialRecovery));

        Assert.True(result.IsHandled);
        Assert.NotNull(result.Response);
        ipWhiteListAppService.Verify(x => x.IsInWhiteListAsync("4.4.4.4"), Times.Once);
        googleAppService.Verify(x => x.IsGoogleRecaptchaOpenAsync("4.4.4.4", OperationType.SocialRecovery),
            Times.Once);
        verifierAppService.Verify(x => x.CountVerifyCodeInterfaceRequestAsync("4.4.4.4"), Times.Once);
    }

    private static VerificationRequestOperationContext CreateContext(OperationType operationType,
        string guardianType = "Email", string guardianIdentifier = "user@example.com")
    {
        return new VerificationRequestOperationContext(null, null, new SendVerificationRequestInput
        {
            Type = guardianType,
            GuardianIdentifier = guardianIdentifier,
            VerifierId = "verifier-id",
            ChainId = "AELF",
            OperationType = operationType,
            PlatformType = PlatformType.WEB
        }, "trace-id");
    }

    private static HandlerSetup CreateHandlerSetup(bool checkSwitchOpen = false)
    {
        var httpContext = new DefaultHttpContext();
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(x => x.HttpContext).Returns(httpContext);
        var clientIpResolver = new TestHttpClientIpResolver(accessor.Object);

        var switchAppService = new Mock<ISwitchAppService>();
        switchAppService.Setup(x => x.GetSwitchStatus(It.IsAny<string>())).Returns((string switchName) => new SwitchDto
        {
            IsOpen = switchName switch
            {
                "CheckSwitch" => checkSwitchOpen,
                "GoogleRecaptcha" => false,
                _ => false
            }
        });

        return new HandlerSetup
        {
            HttpContext = httpContext,
            ClientIpResolver = clientIpResolver,
            VerifierAppService = new Mock<IVerifierAppService>(),
            RateLimitService = new Mock<IRegistrationEmailRateLimitService>(),
            RiskControlService = new Mock<IVerificationRequestRiskControlService>(),
            SwitchAppService = switchAppService
        };
    }

    private class HandlerSetup
    {
        public DefaultHttpContext HttpContext { get; init; }

        public IHttpClientIpResolver ClientIpResolver { get; init; }

        public Mock<IVerifierAppService> VerifierAppService { get; init; }

        public Mock<IRegistrationEmailRateLimitService> RateLimitService { get; init; }

        public Mock<IVerificationRequestRiskControlService> RiskControlService { get; init; }

        public Mock<ISwitchAppService> SwitchAppService { get; init; }
    }

    private sealed class TestHttpClientIpResolver : IHttpClientIpResolver
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TestHttpClientIpResolver(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetForwardedClientIp()
        {
            return GetFirstHeaderIp(ClientIpHeaders.XForwardedFor) ?? GetFirstHeaderIp(ClientIpHeaders.XRealIp);
        }

        public string GetBestEffortClientIp()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                return null;
            }

            if (context.Items.TryGetValue("CAServer:ResolvedClientIp", out var resolvedClientIp) &&
                resolvedClientIp is string requestScopedClientIp &&
                !string.IsNullOrWhiteSpace(requestScopedClientIp))
            {
                return requestScopedClientIp;
            }

            return GetFirstHeaderIp(ClientIpHeaders.XForwardedFor) ??
                   GetFirstHeaderIp(ClientIpHeaders.XRealIp) ??
                   context.Connection.RemoteIpAddress?.ToString();
        }

        public string GetFirstHeaderIp(string headerName)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null || !context.Request.Headers.TryGetValue(headerName, out var headerValue))
            {
                return null;
            }

            foreach (var ip in headerValue.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!string.IsNullOrWhiteSpace(ip))
                {
                    return ip;
                }
            }

            return null;
        }

        public void SetResolvedClientIp(string clientIp)
        {
            if (_httpContextAccessor.HttpContext == null || string.IsNullOrWhiteSpace(clientIp))
            {
                return;
            }

            _httpContextAccessor.HttpContext.Items["CAServer:ResolvedClientIp"] = clientIp;
        }
    }
}
