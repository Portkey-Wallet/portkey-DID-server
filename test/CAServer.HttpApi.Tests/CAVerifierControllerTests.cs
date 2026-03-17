using System;
using System.Net;
using System.Threading.Tasks;
using CAServer;
using CAServer.CAAccount;
using CAServer.Controllers;
using CAServer.Dtos;
using CAServer.Google;
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
        IRegistrationEmailRateLimitService? registrationEmailRateLimitService = null)
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
        switchAppService.Setup(x => x.GetSwitchStatus(It.IsAny<string>())).Returns(new SwitchDto
        {
            IsOpen = false
        });

        var controller = new CAVerifierController(
            verifierAppService ?? Mock.Of<IVerifierAppService>(),
            mapper.Object,
            Mock.Of<ILogger<CAVerifierController>>(),
            switchAppService.Object,
            Mock.Of<IGoogleAppService>(),
            Mock.Of<ICurrentUser>(),
            Mock.Of<IIpWhiteListAppService>(),
            Mock.Of<IZkLoginProvider>(),
            Mock.Of<ISecondaryEmailAppService>(),
            registrationEmailRateLimitService ?? Mock.Of<IRegistrationEmailRateLimitService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }
}
