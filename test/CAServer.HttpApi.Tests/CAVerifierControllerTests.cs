using System;
using System.Net;
using System.Threading.Tasks;
using CAServer.CAAccount;
using CAServer.CAAccount.Cmd;
using CAServer.Controllers;
using CAServer.Dtos;
using CAServer.Google;
using CAServer.Google.Dtos;
using CAServer.IpInfo;
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
using Xunit;

namespace CAServer.HttpApi.Tests;

public class CAVerifierControllerTests
{
    [Fact]
    public async Task SendVerificationRequest_Should_Apply_Dispatcher_Status_And_RetryAfter_When_Handled()
    {
        var dispatcher = new Mock<IVerificationRequestOperationDispatcher>();
        dispatcher.Setup(x => x.HandleAsync(It.IsAny<VerificationRequestOperationContext>()))
            .ReturnsAsync(VerificationRequestOperationResult.Handled(new VerifierServerResponse(),
                HttpStatusCode.TooManyRequests, 321));

        var verifierAppService = new Mock<IVerifierAppService>();
        var controller = CreateController(
            verifierAppService: verifierAppService.Object,
            verificationRequestOperationDispatcher: dispatcher.Object);

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
    public async Task SendVerificationRequest_Should_Use_Legacy_Send_When_Dispatcher_Does_Not_Handle_And_CheckSwitch_Is_Off()
    {
        var dispatcher = new Mock<IVerificationRequestOperationDispatcher>();
        dispatcher.Setup(x => x.HandleAsync(It.IsAny<VerificationRequestOperationContext>()))
            .ReturnsAsync(VerificationRequestOperationResult.NotHandled());

        var verifierAppService = new Mock<IVerifierAppService>();
        verifierAppService.Setup(x => x.SendVerificationRequestAsync(It.IsAny<SendVerificationRequestInput>()))
            .ReturnsAsync(new VerifierServerResponse
            {
                VerifierSessionId = Guid.NewGuid()
            });

        var controller = CreateController(
            verifierAppService: verifierAppService.Object,
            verificationRequestOperationDispatcher: dispatcher.Object,
            checkSwitchOpen: false);

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
    }

    [Fact]
    public async Task SendVerificationRequest_Should_Use_RiskControlService_When_Dispatcher_Does_Not_Handle_And_CheckSwitch_Is_On()
    {
        var dispatcher = new Mock<IVerificationRequestOperationDispatcher>();
        dispatcher.Setup(x => x.HandleAsync(It.IsAny<VerificationRequestOperationContext>()))
            .ReturnsAsync(VerificationRequestOperationResult.NotHandled());

        var riskControlService = new Mock<IVerificationRequestRiskControlService>();
        riskControlService.Setup(x => x.HandleGuardianOperationAsync(null, null,
                It.IsAny<SendVerificationRequestInput>(), OperationType.Approve))
            .ReturnsAsync(RiskControlExecutionResult<VerifierServerResponse>.Handled(new VerifierServerResponse
            {
                VerifierSessionId = Guid.NewGuid()
            }));

        var controller = CreateController(
            verificationRequestOperationDispatcher: dispatcher.Object,
            verificationRequestRiskControlService: riskControlService.Object,
            checkSwitchOpen: true);

        var response = await controller.SendVerificationRequest(null, null, new VerifierServerInput
        {
            Type = "Email",
            GuardianIdentifier = "user@example.com",
            VerifierId = "verifier-id",
            ChainId = "AELF",
            OperationType = OperationType.Approve
        });

        Assert.NotNull(response);
        riskControlService.Verify(x => x.HandleGuardianOperationAsync(null, null,
            It.IsAny<SendVerificationRequestInput>(), OperationType.Approve), Times.Once);
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
    public async Task VerifySecondaryEmailAsync_Should_Apply_RiskControl_Status_And_Response()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);

        var riskControlService = new Mock<IVerificationRequestRiskControlService>();
        riskControlService.Setup(x => x.HandleSecondaryEmailAsync(null, "ac-token", It.IsAny<VerifySecondaryEmailCmd>()))
            .ReturnsAsync(RiskControlExecutionResult<VerifySecondaryEmailResponse>.Handled(
                new VerifySecondaryEmailResponse
                {
                    VerifierSessionId = "session-id"
                },
                HttpStatusCode.Unauthorized));

        var controller = CreateController(
            currentUser: currentUser.Object,
            verificationRequestRiskControlService: riskControlService.Object,
            checkSwitchOpen: true);

        var response = await controller.VerifySecondaryEmailAsync(null, "ac-token", new VerifySecondaryEmailCmd
        {
            SecondaryEmail = "user@example.com",
            PlatformType = PlatformType.WEB
        });

        Assert.NotNull(response);
        Assert.Equal("session-id", response.VerifierSessionId);
        Assert.Equal((int)HttpStatusCode.Unauthorized, controller.HttpContext.Response.StatusCode);
        riskControlService.Verify(x => x.HandleSecondaryEmailAsync(null, "ac-token",
            It.IsAny<VerifySecondaryEmailCmd>()), Times.Once);
    }

    [Fact]
    public async Task VerifySecondaryEmailAsync_Should_Return_401_When_User_Is_Not_Authenticated()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(false);

        var controller = CreateController(currentUser: currentUser.Object, checkSwitchOpen: true);

        var response = await controller.VerifySecondaryEmailAsync(null, null, new VerifySecondaryEmailCmd
        {
            SecondaryEmail = "user@example.com",
            PlatformType = PlatformType.WEB
        });

        Assert.NotNull(response);
        Assert.Equal((int)HttpStatusCode.Unauthorized, controller.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task VerifySecondaryEmailAsync_Should_Bypass_RiskControl_When_CheckSwitch_Is_Off()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);

        var secondaryEmailAppService = new Mock<ISecondaryEmailAppService>();
        secondaryEmailAppService.Setup(x => x.VerifySecondaryEmailAsync(It.IsAny<VerifySecondaryEmailCmd>()))
            .ReturnsAsync(new VerifySecondaryEmailResponse
            {
                VerifierSessionId = "session-id"
            });

        var controller = CreateController(
            currentUser: currentUser.Object,
            secondaryEmailAppService: secondaryEmailAppService.Object,
            checkSwitchOpen: false);

        var response = await controller.VerifySecondaryEmailAsync(null, null, new VerifySecondaryEmailCmd
        {
            SecondaryEmail = "user@example.com",
            PlatformType = PlatformType.WEB
        });

        Assert.NotNull(response);
        Assert.Equal("session-id", response.VerifierSessionId);
        secondaryEmailAppService.Verify(x => x.VerifySecondaryEmailAsync(It.IsAny<VerifySecondaryEmailCmd>()),
            Times.Once);
    }

    private static CAVerifierController CreateController(
        IVerifierAppService verifierAppService = null,
        IVerificationRequestOperationDispatcher verificationRequestOperationDispatcher = null,
        IVerificationRequestRiskControlService verificationRequestRiskControlService = null,
        IGoogleAppService googleAppService = null,
        ICurrentUser currentUser = null,
        IIpWhiteListAppService ipWhiteListAppService = null,
        ISecondaryEmailAppService secondaryEmailAppService = null,
        bool checkSwitchOpen = false,
        bool googleRecaptchaSwitchOpen = false,
        string remoteIpAddress = null)
    {
        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrWhiteSpace(remoteIpAddress))
        {
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(remoteIpAddress);
        }

        var clientIpResolver = TestHttpClientIpResolverFactory.Create(httpContext);

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
                VerificationSwitchNames.CheckSwitch => checkSwitchOpen,
                VerificationSwitchNames.GoogleRecaptcha => googleRecaptchaSwitchOpen,
                _ => false
            }
        });

        var controller = new CAVerifierController(
            verifierAppService ?? Mock.Of<IVerifierAppService>(),
            clientIpResolver,
            mapper.Object,
            Mock.Of<ILogger<CAVerifierController>>(),
            switchAppService.Object,
            googleAppService ?? Mock.Of<IGoogleAppService>(),
            currentUser ?? Mock.Of<ICurrentUser>(),
            ipWhiteListAppService ?? Mock.Of<IIpWhiteListAppService>(),
            Mock.Of<IZkLoginProvider>(),
            secondaryEmailAppService ?? Mock.Of<ISecondaryEmailAppService>(),
            verificationRequestOperationDispatcher ?? Mock.Of<IVerificationRequestOperationDispatcher>(),
            verificationRequestRiskControlService ?? Mock.Of<IVerificationRequestRiskControlService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        return controller;
    }
}
