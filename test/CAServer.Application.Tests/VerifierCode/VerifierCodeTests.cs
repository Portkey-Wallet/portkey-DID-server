using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CAServer.VerifierCode;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class VerifierCodeTests : CAServerApplicationTestBase
{
    private readonly IVerifierAppService _verifierAppService;
    private const string PhoneType = "Phone";
    private const string EmailType = "Email";
    private const string InvalidType = "Invalid";
    private const string FakePhoneNum = "+861234567890";
    private const string FakeEmail = "1@google.com";
    private const string FakeInvalidateEmail = "1googlecom";
    private const string DefaultChainId = "AELF";
    private const string SideChainId = "TDVV";
    private const string DefaultVerifierId = "50986afa3095f55bd590d6ab26218cc2ed2ef8b1f6e7cdab5b3cbb2cd8a540f5";
    private const string DefaultVerifierCode = "123456";
    private JwtSecurityTokenHandler _jwtSecurityTokenHandler;

    public VerifierCodeTests()
    {
        _verifierAppService = GetRequiredService<IVerifierAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        _jwtSecurityTokenHandler = Substitute.For<JwtSecurityTokenHandler>();
        services.AddSingleton(GetMockVerifierServerClient());
        services.AddSingleton(GetMockHttpClientFactory());
        services.AddSingleton(_jwtSecurityTokenHandler);
        services.AddSingleton(GetJwtSecurityTokenHandlerMock());
        services.AddSingleton(GetHttpClientService());
        services.AddSingleton(GetMockCacheProvider());
    }

    [Fact]
    public async Task Send_VerifyCode_InvalidateType_Test()
    {
        var input = new SendVerificationRequestInput
        {
            ChainId = DefaultChainId,
            Type = InvalidType,
            GuardianIdentifier = FakeEmail,
            VerifierId = DefaultVerifierId
        };
        try
        {
            await _verifierAppService.SendVerificationRequestAsync(input);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe("InvalidInput type.");
        }
    }

    [Fact]
    public async Task Send_VerifyCode_InvalidateGuardianIdentifier_Test()
    {
        var requestInput = new SendVerificationRequestInput
        {
            ChainId = DefaultChainId,
            Type = EmailType,
            GuardianIdentifier = FakeInvalidateEmail,
            VerifierId = DefaultVerifierId
        };
        try
        {
            await _verifierAppService.SendVerificationRequestAsync(requestInput);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe("InvalidInput GuardianIdentifier");
        }
    }

    [Fact]
    public async Task Send_VerifyCode_Test()
    {
        var requestInput = new SendVerificationRequestInput
        {
            ChainId = DefaultChainId,
            Type = EmailType,
            GuardianIdentifier = FakeEmail,
            VerifierId = DefaultVerifierId
        };
        var response = await _verifierAppService.SendVerificationRequestAsync(requestInput);
        response.ShouldNotBeNull();

        var input = new SendVerificationRequestInput
        {
            ChainId = DefaultChainId,
            Type = PhoneType,
            GuardianIdentifier = FakePhoneNum,
            VerifierId = DefaultVerifierId,
        };
        try
        {
            await _verifierAppService.SendVerificationRequestAsync(input);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe("Send VerifierCode Failed.");
        }
    }

    [Fact]
    public async Task VerifyCodeAsync_Test()
    {
        var input = new VerificationSignatureRequestDto
        {
            VerificationCode = DefaultVerifierCode,
            VerifierSessionId = Guid.NewGuid().ToString(),
            GuardianIdentifier = FakeEmail,
            VerifierId = DefaultVerifierId,
            ChainId = DefaultChainId,
            OperationType = OperationType.AddGuardian
        };
        var response = await _verifierAppService.VerifyCodeAsync(input);
        response.VerificationDoc.ShouldBe("verificationDoc");
        response.Signature.ShouldBe("signature");

        var requestDto = new VerificationSignatureRequestDto
        {
            VerificationCode = DefaultVerifierCode,
            VerifierSessionId = Guid.NewGuid().ToString(),
            GuardianIdentifier = FakeEmail,
            VerifierId = DefaultVerifierId,
            ChainId = SideChainId,
            OperationType = OperationType.AddGuardian
        };
        try
        {
            await _verifierAppService.VerifyCodeAsync(requestDto);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe("Verify VerifierCode Failed.");
        }
    }

    [Fact]
    public async Task VerifierGoogle_Tests()
    {
        var requestDto = new VerifyTokenRequestDto
        {
            VerifierId = DefaultVerifierId,
            ChainId = DefaultChainId,
            AccessToken = "123456"
        };
        await _verifierAppService.VerifyGoogleTokenAsync(requestDto);
       
    }

    [Fact]
    public async Task VerifierApple_Tests()
    {
        var requestDto = new VerifyTokenRequestDto
        {
            VerifierId = DefaultVerifierId,
            ChainId = DefaultChainId,
            AccessToken =
                "eyJraWQiOiJXNldjT0tCIiwiYWxnIjoiUlMyNTYifQ.eyJpc3MiOiJodHRwczovL2FwcGxlaWQuYXBwbGUuY29tIiwiYXVkIjoiY29tLnBvcnRrZXkuZGlkIiwiZXhwIjoxNjc5NDcyMjg1LCJpYXQiOjE2NzkzODU4ODUsInN1YiI6IjAwMDMwMy5jZDgxN2I2OTgzMDc0ZDhjOGZiNzkyNDk2ZjI3N2ViYy4wMjU3IiwiY19oYXNoIjoicFBSeFFTSWNWY19BTEExSE9vdmJ5QSIsImVtYWlsIjoicHQ2eXhtOXptbUBwcml2YXRlcmVsYXkuYXBwbGVpZC5jb20iLCJlbWFpbF92ZXJpZmllZCI6InRydWUiLCJpc19wcml2YXRlX2VtYWlsIjoidHJ1ZSIsImF1dGhfdGltZSI6MTY3OTM4NTg4NSwibm9uY2Vfc3VwcG9ydGVkIjp0cnVlfQ.wXHXNbQVqvRxK_a6dq3WjBbJe_KaGsRVgSz_i3E01JyKW8rxGRRgDqjYNiTxB6iOqBMfvXfjtjgPl1N-de_Q4OflzG7gKK_17c-sY2uXUbOWVtAFI9WEXksYhZdV66eJDiUKJ8KE94S6NCT8UdkRqtxHtCnjuq82taYPbqcb-NO3Xcu23hfKsYQM_73yHJfnFd7jUYCoLHcxlVUeRGR7D7L3Yo9FdbocHZwei_x_jwb_7gYjTqGKg6rYt4MRT5ElSTj4xajXrRLZZzCFTVPjytvUsGvU038SEj4sIK6eoDAQy90ne2_XritzViMfKcWid6cdgh-Zz3PzfRx9LEyIPg"
        };

        await _verifierAppService.VerifyAppleTokenAsync(requestDto);
    }

    [Fact]
    public async Task CountVerifyCodeInterfaceRequest_Tests()
    {
        var userIpAddress = "127.0.0.1";
        var count = await _verifierAppService.CountVerifyCodeInterfaceRequestAsync(userIpAddress);
        count.ShouldBe(0);
    }



}