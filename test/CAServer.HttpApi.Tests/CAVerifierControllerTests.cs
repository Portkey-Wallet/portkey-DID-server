using System;
using System.Net;
using System.Threading.Tasks;
using CAServer;
using CAServer.CAAccount;
using CAServer.CAAccount.Cmd;
using CAServer.Controllers;
using CAServer.Dtos;
using CAServer.Google;
using CAServer.Google.Dtos;
using CAServer.IpWhiteList;
using CAServer.Switch;
using CAServer.Switch.Dtos;
using CAServer.Verifier;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace CAServer.HttpApi.Tests;

public class CAVerifierControllerTests
{
    [Fact]
    public async Task SendVerificationRequest_Should_Return_400_When_InScope_And_Ip_Headers_Are_Missing()
    {
        var verifierAppService = new Mock<IVerifierAppService>();
        var rateLimitService = new Mock<IRegistrationEmailRateLimitService>();
        rateLimitService.Setup(x => x.ShouldApply("Email", OperationType.CreateCAHolder)).Returns(true);
        var controller = CreateController(verifierAppService: verifierAppService.Object,
            registrationEmailRateLimitService: rateLimitService.Object);

        var response = await controller.SendVerificationRequest(null, null, new VerifierServerInput
        {
            Type = "Email",
            GuardianIdentifier = "user@example.com",
            VerifierId = "verifier-id",
            ChainId = "AELF",
            OperationType = OperationType.CreateCAHolder
        });

        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.BadRequest, controller.HttpContext.Response.StatusCode);
        verifierAppService.Verify(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()),
            Times.Never);
        rateLimitService.Verify(x => x.CheckAsync(It.IsAny<string>(), It.IsAny<OperationType>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task SendVerificationRequest_Should_Use_First_XForwardedFor_Ip()
    {
        var verifierAppService = new Mock<IVerifierAppService>();
        verifierAppService.Setup(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()))
            .ReturnsAsync(new VerifierServerResponse());
        var rateLimitService = new Mock<IRegistrationEmailRateLimitService>();
        rateLimitService.Setup(x => x.ShouldApply("Email", OperationType.CreateCAHolder)).Returns(true);
        rateLimitService.Setup(x => x.CheckAsync("1.1.1.1", OperationType.CreateCAHolder, It.IsAny<string>()))
            .ReturnsAsync(RegistrationEmailRateLimitCheckResult.Allow());
        var controller = CreateController(verifierAppService.Object, rateLimitService.Object);
        controller.HttpContext.Request.Headers[RequestIpHeaderHelper.XForwardedFor] = "1.1.1.1, 2.2.2.2";
        controller.HttpContext.Request.Headers[RequestIpHeaderHelper.XRealIp] = "3.3.3.3";

        await controller.SendVerificationRequest(null, null, new VerifierServerInput
        {
            Type = "Email",
            GuardianIdentifier = "user@example.com",
            VerifierId = "verifier-id",
            ChainId = "AELF",
            OperationType = OperationType.CreateCAHolder
        });

        rateLimitService.Verify(x => x.CheckAsync("1.1.1.1", OperationType.CreateCAHolder, It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task SendVerificationRequest_Should_Fallback_To_XRealIp()
    {
        var verifierAppService = new Mock<IVerifierAppService>();
        verifierAppService.Setup(x => x.GuardianExistsAsync("user@example.com")).ReturnsAsync(true);
        verifierAppService.Setup(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()))
            .ReturnsAsync(new VerifierServerResponse());
        var rateLimitService = new Mock<IRegistrationEmailRateLimitService>();
        rateLimitService.Setup(x => x.ShouldApply("Email", OperationType.SocialRecovery)).Returns(true);
        rateLimitService.Setup(x => x.CheckAsync("4.4.4.4", OperationType.SocialRecovery, It.IsAny<string>()))
            .ReturnsAsync(RegistrationEmailRateLimitCheckResult.Allow());
        var controller = CreateController(verifierAppService.Object, rateLimitService.Object);
        controller.HttpContext.Request.Headers[RequestIpHeaderHelper.XRealIp] = "4.4.4.4";

        await controller.SendVerificationRequest(null, null, new VerifierServerInput
        {
            Type = "Email",
            GuardianIdentifier = "user@example.com",
            VerifierId = "verifier-id",
            ChainId = "AELF",
            OperationType = OperationType.SocialRecovery
        });

        rateLimitService.Verify(x => x.CheckAsync("4.4.4.4", OperationType.SocialRecovery, It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task SendVerificationRequest_Should_Not_Consume_SocialRecovery_Quota_When_Guardian_Does_Not_Exist()
    {
        var verifierAppService = new Mock<IVerifierAppService>();
        verifierAppService.Setup(x => x.GuardianExistsAsync("missing@example.com")).ReturnsAsync(false);
        var rateLimitService = new Mock<IRegistrationEmailRateLimitService>();
        var controller = CreateController(verifierAppService: verifierAppService.Object,
            registrationEmailRateLimitService: rateLimitService.Object, checkSwitchOpen: true);

        var response = await controller.SendVerificationRequest(null, null, new VerifierServerInput
        {
            Type = "Email",
            GuardianIdentifier = "missing@example.com",
            VerifierId = "verifier-id",
            ChainId = "AELF",
            OperationType = OperationType.SocialRecovery
        });

        Assert.Null(response);
        rateLimitService.Verify(x => x.ShouldApply(It.IsAny<string>(), It.IsAny<OperationType>()), Times.Never);
        rateLimitService.Verify(x => x.CheckAsync(It.IsAny<string>(), It.IsAny<OperationType>(), It.IsAny<string>()),
            Times.Never);
        verifierAppService.Verify(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()),
            Times.Never);
    }

    [Fact]
    public async Task SendVerificationRequest_Should_Return_400_When_Only_RemoteIpAddress_Is_Present_For_InScope_Request()
    {
        var verifierAppService = new Mock<IVerifierAppService>();
        var rateLimitService = new Mock<IRegistrationEmailRateLimitService>();
        rateLimitService.Setup(x => x.ShouldApply("Email", OperationType.CreateCAHolder)).Returns(true);
        var controller = CreateController(verifierAppService: verifierAppService.Object,
            registrationEmailRateLimitService: rateLimitService.Object, remoteIpAddress: "6.6.6.6");

        var response = await controller.SendVerificationRequest(null, null, new VerifierServerInput
        {
            Type = "Email",
            GuardianIdentifier = "user@example.com",
            VerifierId = "verifier-id",
            ChainId = "AELF",
            OperationType = OperationType.CreateCAHolder
        });

        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.BadRequest, controller.HttpContext.Response.StatusCode);
        rateLimitService.Verify(x => x.CheckAsync(It.IsAny<string>(), It.IsAny<OperationType>(), It.IsAny<string>()),
            Times.Never);
        verifierAppService.Verify(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()),
            Times.Never);
    }

    [Fact]
    public async Task SendVerificationRequest_Should_Return_429_And_RetryAfter_When_Limited()
    {
        var verifierAppService = new Mock<IVerifierAppService>();
        var rateLimitService = new Mock<IRegistrationEmailRateLimitService>();
        rateLimitService.Setup(x => x.ShouldApply("Email", OperationType.CreateCAHolder)).Returns(true);
        rateLimitService.Setup(x => x.CheckAsync("5.5.5.5", OperationType.CreateCAHolder, It.IsAny<string>()))
            .ReturnsAsync(RegistrationEmailRateLimitCheckResult.Block("CreateCAHolder:10m", 10, 11, 0, 321));
        var controller = CreateController(verifierAppService.Object, rateLimitService.Object);
        controller.HttpContext.Request.Headers[RequestIpHeaderHelper.XForwardedFor] = "5.5.5.5";

        var response = await controller.SendVerificationRequest(null, null, new VerifierServerInput
        {
            Type = "Email",
            GuardianIdentifier = "user@example.com",
            VerifierId = "verifier-id",
            ChainId = "AELF",
            OperationType = OperationType.CreateCAHolder
        });

        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.TooManyRequests, controller.HttpContext.Response.StatusCode);
        Assert.Equal("321", controller.HttpContext.Response.Headers["Retry-After"].ToString());
        verifierAppService.Verify(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()),
            Times.Never);
    }

    [Fact]
    public async Task SendVerificationRequest_Should_Not_Require_Captcha_For_CreateCAHolder_When_CheckSwitch_Is_On()
    {
        var verifierAppService = new Mock<IVerifierAppService>();
        verifierAppService.Setup(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()))
            .ReturnsAsync(new VerifierServerResponse
            {
                VerifierSessionId = Guid.NewGuid()
            });

        var rateLimitService = new Mock<IRegistrationEmailRateLimitService>();
        rateLimitService.Setup(x => x.ShouldApply("Email", OperationType.CreateCAHolder)).Returns(true);
        rateLimitService.Setup(x => x.CheckAsync("9.9.9.9", OperationType.CreateCAHolder, It.IsAny<string>()))
            .ReturnsAsync(RegistrationEmailRateLimitCheckResult.Allow());

        var googleAppService = new Mock<IGoogleAppService>();

        var controller = CreateController(
            verifierAppService: verifierAppService.Object,
            registrationEmailRateLimitService: rateLimitService.Object,
            googleAppService: googleAppService.Object,
            checkSwitchOpen: true,
            googleRecaptchaSwitchOpen: true);
        controller.HttpContext.Request.Headers[RequestIpHeaderHelper.XForwardedFor] = "9.9.9.9";

        var response = await controller.SendVerificationRequest(null, null, new VerifierServerInput
        {
            Type = "Email",
            GuardianIdentifier = "user@example.com",
            VerifierId = "verifier-id",
            ChainId = "AELF",
            OperationType = OperationType.CreateCAHolder
        });

        Assert.NotNull(response);
        Assert.Equal(StatusCodes.Status200OK, controller.HttpContext.Response.StatusCode);
        verifierAppService.Verify(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()),
            Times.Once);
        verifierAppService.Verify(x => x.CountVerifyCodeInterfaceRequestAsync(It.IsAny<string>()), Times.Never);
        googleAppService.Verify(x => x.IsGoogleRecaptchaOpenAsync(It.IsAny<string>(), It.IsAny<OperationType>()),
            Times.Never);
        googleAppService.Verify(
            x => x.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PlatformType>()),
            Times.Never);
    }

    [Fact]
    public async Task SendVerificationRequest_Should_Reuse_Resolved_XRealIp_For_SocialRecovery_Risk_Control()
    {
        var verifierAppService = new Mock<IVerifierAppService>();
        verifierAppService.Setup(x => x.GuardianExistsAsync("user@example.com")).ReturnsAsync(true);
        verifierAppService.Setup(x => x.CountVerifyCodeInterfaceRequestAsync("4.4.4.4")).ReturnsAsync(1);
        verifierAppService.Setup(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()))
            .ReturnsAsync(new VerifierServerResponse
            {
                VerifierSessionId = Guid.NewGuid()
            });

        var rateLimitService = new Mock<IRegistrationEmailRateLimitService>();
        rateLimitService.Setup(x => x.ShouldApply("Email", OperationType.SocialRecovery)).Returns(true);
        rateLimitService.Setup(x => x.CheckAsync("4.4.4.4", OperationType.SocialRecovery, It.IsAny<string>()))
            .ReturnsAsync(RegistrationEmailRateLimitCheckResult.Allow());

        var ipWhiteListAppService = new Mock<IIpWhiteListAppService>();
        ipWhiteListAppService.Setup(x => x.IsInWhiteListAsync("4.4.4.4")).ReturnsAsync(true);

        var googleAppService = new Mock<IGoogleAppService>();
        googleAppService.Setup(x => x.IsGoogleRecaptchaOpenAsync("4.4.4.4", OperationType.SocialRecovery))
            .ReturnsAsync(false);

        var controller = CreateController(
            verifierAppService: verifierAppService.Object,
            registrationEmailRateLimitService: rateLimitService.Object,
            googleAppService: googleAppService.Object,
            ipWhiteListAppService: ipWhiteListAppService.Object,
            checkSwitchOpen: true,
            googleRecaptchaSwitchOpen: false);
        controller.HttpContext.Request.Headers[RequestIpHeaderHelper.XRealIp] = "4.4.4.4";

        var response = await controller.SendVerificationRequest(null, null, new VerifierServerInput
        {
            Type = "Email",
            GuardianIdentifier = "user@example.com",
            VerifierId = "verifier-id",
            ChainId = "AELF",
            OperationType = OperationType.SocialRecovery
        });

        Assert.NotNull(response);
        Assert.Equal(StatusCodes.Status200OK, controller.HttpContext.Response.StatusCode);
        rateLimitService.Verify(x => x.CheckAsync("4.4.4.4", OperationType.SocialRecovery, It.IsAny<string>()),
            Times.Once);
        ipWhiteListAppService.Verify(x => x.IsInWhiteListAsync("4.4.4.4"), Times.Once);
        googleAppService.Verify(x => x.IsGoogleRecaptchaOpenAsync("4.4.4.4", OperationType.SocialRecovery),
            Times.Once);
        verifierAppService.Verify(x => x.CountVerifyCodeInterfaceRequestAsync("4.4.4.4"), Times.Once);
    }

    [Fact]
    public async Task SendVerificationRequest_Should_Bypass_Hard_Limit_For_Approve()
    {
        var verifierAppService = new Mock<IVerifierAppService>();
        verifierAppService.Setup(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()))
            .ReturnsAsync(new VerifierServerResponse
            {
                VerifierSessionId = Guid.NewGuid()
            });
        var rateLimitService = new Mock<IRegistrationEmailRateLimitService>();
        rateLimitService.Setup(x => x.ShouldApply("Email", OperationType.Approve)).Returns(false);
        var controller = CreateController(verifierAppService.Object, rateLimitService.Object);

        var response = await controller.SendVerificationRequest(null, null, new VerifierServerInput
        {
            Type = "Email",
            GuardianIdentifier = "user@example.com",
            VerifierId = "verifier-id",
            ChainId = "AELF",
            OperationType = OperationType.Approve
        });

        Assert.NotNull(response);
        Assert.Equal(StatusCodes.Status200OK, controller.HttpContext.Response.StatusCode);
        verifierAppService.Verify(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()),
            Times.Once);
        rateLimitService.Verify(x => x.CheckAsync(It.IsAny<string>(), It.IsAny<OperationType>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task IsGoogleRecaptchaOpen_Should_Use_RemoteIpAddress_Fallback_When_Headers_Are_Missing()
    {
        var ipWhiteListAppService = new Mock<IIpWhiteListAppService>();
        ipWhiteListAppService.Setup(x => x.IsInWhiteListAsync("7.7.7.7")).ReturnsAsync(false);
        var controller = CreateController(ipWhiteListAppService: ipWhiteListAppService.Object,
            checkSwitchOpen: true, remoteIpAddress: "7.7.7.7");

        var response = await controller.IsGoogleRecaptchaOpen(null, new OperationTypeRequestInput
        {
            OperationType = OperationType.CreateCAHolder
        });

        Assert.True(response);
        ipWhiteListAppService.Verify(x => x.IsInWhiteListAsync("7.7.7.7"), Times.Once);
    }

    [Fact]
    public async Task VerifySecondaryEmailAsync_Should_Use_RemoteIpAddress_Fallback_When_Headers_Are_Missing()
    {
        var verifierAppService = new Mock<IVerifierAppService>();
        verifierAppService.Setup(x => x.CountVerifyCodeInterfaceRequestAsync("8.8.8.8")).ReturnsAsync(1);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);

        var ipWhiteListAppService = new Mock<IIpWhiteListAppService>();
        ipWhiteListAppService.Setup(x => x.IsInWhiteListAsync("8.8.8.8")).ReturnsAsync(false);

        var googleAppService = new Mock<IGoogleAppService>();
        googleAppService.Setup(x => x.ValidateTokenAsync(null, "ac-token", PlatformType.WEB))
            .ReturnsAsync(new ValidateTokenResponse
            {
                AcValidResult = true
            });

        var secondaryEmailAppService = new Mock<ISecondaryEmailAppService>();
        secondaryEmailAppService.Setup(x => x.VerifySecondaryEmailAsync(It.IsAny<VerifySecondaryEmailCmd>()))
            .ReturnsAsync(new VerifySecondaryEmailResponse
            {
                VerifierSessionId = "session-id"
            });

        var controller = CreateController(verifierAppService: verifierAppService.Object,
            googleAppService: googleAppService.Object,
            currentUser: currentUser.Object,
            ipWhiteListAppService: ipWhiteListAppService.Object,
            secondaryEmailAppService: secondaryEmailAppService.Object,
            checkSwitchOpen: true,
            remoteIpAddress: "8.8.8.8");

        var response = await controller.VerifySecondaryEmailAsync(null, "ac-token", new VerifySecondaryEmailCmd
        {
            SecondaryEmail = "user@example.com",
            PlatformType = PlatformType.WEB
        });

        Assert.NotNull(response);
        Assert.Equal("session-id", response.VerifierSessionId);
        verifierAppService.Verify(x => x.CountVerifyCodeInterfaceRequestAsync("8.8.8.8"), Times.Once);
        secondaryEmailAppService.Verify(x => x.VerifySecondaryEmailAsync(It.IsAny<VerifySecondaryEmailCmd>()),
            Times.Once);
    }

    [Fact]
    public async Task SendVerificationRequest_Should_Bypass_Hard_Limit_When_Feature_Is_Disabled()
    {
        var verifierAppService = new Mock<IVerifierAppService>();
        verifierAppService.Setup(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()))
            .ReturnsAsync(new VerifierServerResponse
            {
                VerifierSessionId = Guid.NewGuid()
            });
        var rateLimitService = new Mock<IRegistrationEmailRateLimitService>();
        rateLimitService.Setup(x => x.ShouldApply("Email", OperationType.CreateCAHolder)).Returns(false);
        var controller = CreateController(verifierAppService.Object, rateLimitService.Object);

        var response = await controller.SendVerificationRequest(null, null, new VerifierServerInput
        {
            Type = "Email",
            GuardianIdentifier = "user@example.com",
            VerifierId = "verifier-id",
            ChainId = "AELF",
            OperationType = OperationType.CreateCAHolder
        });

        Assert.NotNull(response);
        Assert.Equal(StatusCodes.Status200OK, controller.HttpContext.Response.StatusCode);
        verifierAppService.Verify(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()),
            Times.Once);
        rateLimitService.Verify(x => x.CheckAsync(It.IsAny<string>(), It.IsAny<OperationType>(), It.IsAny<string>()),
            Times.Never);
    }

    private static CAVerifierController CreateController(
        IVerifierAppService? verifierAppService = null,
        IRegistrationEmailRateLimitService? registrationEmailRateLimitService = null,
        IGoogleAppService? googleAppService = null,
        ICurrentUser? currentUser = null,
        IIpWhiteListAppService? ipWhiteListAppService = null,
        ISecondaryEmailAppService? secondaryEmailAppService = null,
        bool checkSwitchOpen = false,
        bool googleRecaptchaSwitchOpen = false,
        string? remoteIpAddress = null)
    {
        var mapper = new Mock<IObjectMapper>();
        mapper.Setup(x => x.Map<VerifierServerInput, SendVerificationRequestInput>(It.IsAny<VerifierServerInput>()))
            .Returns((VerifierServerInput input) => new SendVerificationRequestInput
            {
                Type = input.Type,
                GuardianIdentifier = input.GuardianIdentifier,
                VerifierId = input.VerifierId,
                ChainId = input.ChainId,
                OperationType = input.OperationType,
                PlatformType = input.PlatformType,
                OperationDetails = input.OperationDetails,
                TargetChainId = input.TargetChainId
            });

        var switchAppService = new Mock<ISwitchAppService>();
        switchAppService.Setup(x => x.GetSwitchStatus(It.IsAny<string>())).Returns((string switchName) => new SwitchDto
        {
            IsOpen = switchName switch
            {
                "CheckSwitch" => checkSwitchOpen,
                "GoogleRecaptcha" => googleRecaptchaSwitchOpen,
                _ => false
            }
        });

        var controller = new CAVerifierController(
            verifierAppService ?? Mock.Of<IVerifierAppService>(),
            mapper.Object,
            Mock.Of<ILogger<CAVerifierController>>(),
            switchAppService.Object,
            googleAppService ?? Mock.Of<IGoogleAppService>(),
            currentUser ?? Mock.Of<ICurrentUser>(),
            ipWhiteListAppService ?? Mock.Of<IIpWhiteListAppService>(),
            Mock.Of<IZkLoginProvider>(),
            secondaryEmailAppService ?? Mock.Of<ISecondaryEmailAppService>(),
            registrationEmailRateLimitService ?? Mock.Of<IRegistrationEmailRateLimitService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        if (!string.IsNullOrWhiteSpace(remoteIpAddress))
        {
            controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse(remoteIpAddress);
        }

        return controller;
    }
}
