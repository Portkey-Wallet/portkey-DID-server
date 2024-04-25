using System;
using System.Threading.Tasks;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CAServer.VerifierCode;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class VerifierServerClientTests : CAServerApplicationTestBase
{
    private readonly IVerifierServerClient _verifierServerClient;
    private const string DefaultType = "Phone";
    private const string FadePhoneNum = "+861234567890";
    private const string DefaultChainId = "AELF";
    private const string DefaultVerifierId = "50986afa3095f55bd590d6ab26218cc2ed2ef8b1f6e7cdab5b3cbb2cd8a540f5";
    private const string SideChainId = "TDVV";
    private const string DefaultCode = "123456";


    public VerifierServerClientTests()
    {
        _verifierServerClient = GetRequiredService<IVerifierServerClient>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetVerifierServerProvider());
        services.AddSingleton(GetAdaptableVariableOptions());
        // services.AddSingleton(GetMockHttpClient());
        services.AddSingleton(GetMockHttpClientFactory());
    }


    [Fact]
    public async Task SendVerificationRequest_Test()
    {
        var request = new VerifierCodeRequestDto
        {
            Type = DefaultType,
            GuardianIdentifier = FadePhoneNum,
            VerifierSessionId = Guid.NewGuid(),
            VerifierId = DefaultVerifierId,
            ChainId = SideChainId
        };
        var resultDto = await _verifierServerClient.SendVerificationRequestAsync(request);
        resultDto.Success.ShouldBe(false);
        resultDto.Message.ShouldBe("No Available Service Tips.");

        var dto = new VerifierCodeRequestDto
        {
            Type = DefaultType,
            GuardianIdentifier = FadePhoneNum,
            VerifierSessionId = Guid.NewGuid(),
            VerifierId = DefaultVerifierId,
            ChainId = DefaultChainId
        };
        try
        {
            await _verifierServerClient.SendVerificationRequestAsync(dto);
        }
        catch (Exception e)
        {
            e.Message.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task VerifyCode_Test()
    {
        var request = new VierifierCodeRequestInput
        {
            GuardianIdentifier = FadePhoneNum,
            VerifierSessionId = Guid.NewGuid().ToString(),
            VerificationCode = DefaultCode,
            GuardianIdentifierHash = "",
            Salt = "",
            VerifierId = DefaultVerifierId,
            ChainId = SideChainId
        };
        var resultDto = await _verifierServerClient.VerifyCodeAsync(request);
        resultDto.Success.ShouldBe(false);
        resultDto.Message.ShouldBe("No Available Service Tips.");

        var dto = new VierifierCodeRequestInput
        {
            VerifierId = DefaultVerifierId,
            ChainId = DefaultChainId,
            GuardianIdentifier = FadePhoneNum,
            VerifierSessionId = Guid.NewGuid().ToString(),
            VerificationCode = DefaultCode,
            GuardianIdentifierHash = "",
            Salt = ""
        };
        try
        {
            await _verifierServerClient.VerifyCodeAsync(dto);
        }
        catch (Exception e)
        {
            e.Message.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task VerifyGoogleToken_Test()
    {
        var input = new VerifyTokenRequestDto()
        {
            VerifierId = DefaultVerifierId,
            ChainId = DefaultChainId,
            AccessToken = ""
        };
        var identifierHash = "123456";
        var salt = "123456";

        await _verifierServerClient.VerifyGoogleTokenAsync(input, identifierHash, salt);
    }


    [Fact]
    public async Task VerifyAppleToken_Test()
    {
        var input = new VerifyTokenRequestDto()
        {
            VerifierId = DefaultVerifierId,
            ChainId = DefaultChainId,
            AccessToken =
                "eyJraWQiOiJXNldjT0tCIiwiYWxnIjoiUlMyNTYifQ.eyJpc3MiOiJodHRwczovL2FwcGxlaWQuYXBwbGUuY29tIiwiYXVkIjoiY29tLnBvcnRrZXkuZGlkIiwiZXhwIjoxNjc5NDcyMjg1LCJpYXQiOjE2NzkzODU4ODUsInN1YiI6IjAwMDMwMy5jZDgxN2I2OTgzMDc0ZDhjOGZiNzkyNDk2ZjI3N2ViYy4wMjU3IiwiY19oYXNoIjoicFBSeFFTSWNWY19BTEExSE9vdmJ5QSIsImVtYWlsIjoicHQ2eXhtOXptbUBwcml2YXRlcmVsYXkuYXBwbGVpZC5jb20iLCJlbWFpbF92ZXJpZmllZCI6InRydWUiLCJpc19wcml2YXRlX2VtYWlsIjoidHJ1ZSIsImF1dGhfdGltZSI6MTY3OTM4NTg4NSwibm9uY2Vfc3VwcG9ydGVkIjp0cnVlfQ.wXHXNbQVqvRxK_a6dq3WjBbJe_KaGsRVgSz_i3E01JyKW8rxGRRgDqjYNiTxB6iOqBMfvXfjtjgPl1N-de_Q4OflzG7gKK_17c-sY2uXUbOWVtAFI9WEXksYhZdV66eJDiUKJ8KE94S6NCT8UdkRqtxHtCnjuq82taYPbqcb-NO3Xcu23hfKsYQM_73yHJfnFd7jUYCoLHcxlVUeRGR7D7L3Yo9FdbocHZwei_x_jwb_7gYjTqGKg6rYt4MRT5ElSTj4xajXrRLZZzCFTVPjytvUsGvU038SEj4sIK6eoDAQy90ne2_XritzViMfKcWid6cdgh-Zz3PzfRx9LEyIPg"
        };
        var identifierHash = "123456";
        var salt = "123456";

        await _verifierServerClient.VerifyAppleTokenAsync(input, identifierHash, salt);
    }
    
    [Fact]
    public async Task VerifyRevokeToken_Test()
    {
        var input = new VerifyRevokeCodeInput()
        {
            VerifierId = DefaultVerifierId,
            ChainId = DefaultChainId,
            VerifyCode = 
                "eyJraWQiOiJXNldjT0tCIiwiYWxnIjoiUlMyNTYifQ.eyJpc3MiOiJodHRwczovL2FwcGxlaWQuYXBwbGUuY29tIiwiYXVkIjoiY29tLnBvcnRrZXkuZGlkIiwiZXhwIjoxNjc5NDcyMjg1LCJpYXQiOjE2NzkzODU4ODUsInN1YiI6IjAwMDMwMy5jZDgxN2I2OTgzMDc0ZDhjOGZiNzkyNDk2ZjI3N2ViYy4wMjU3IiwiY19oYXNoIjoicFBSeFFTSWNWY19BTEExSE9vdmJ5QSIsImVtYWlsIjoicHQ2eXhtOXptbUBwcml2YXRlcmVsYXkuYXBwbGVpZC5jb20iLCJlbWFpbF92ZXJpZmllZCI6InRydWUiLCJpc19wcml2YXRlX2VtYWlsIjoidHJ1ZSIsImF1dGhfdGltZSI6MTY3OTM4NTg4NSwibm9uY2Vfc3VwcG9ydGVkIjp0cnVlfQ.wXHXNbQVqvRxK_a6dq3WjBbJe_KaGsRVgSz_i3E01JyKW8rxGRRgDqjYNiTxB6iOqBMfvXfjtjgPl1N-de_Q4OflzG7gKK_17c-sY2uXUbOWVtAFI9WEXksYhZdV66eJDiUKJ8KE94S6NCT8UdkRqtxHtCnjuq82taYPbqcb-NO3Xcu23hfKsYQM_73yHJfnFd7jUYCoLHcxlVUeRGR7D7L3Yo9FdbocHZwei_x_jwb_7gYjTqGKg6rYt4MRT5ElSTj4xajXrRLZZzCFTVPjytvUsGvU038SEj4sIK6eoDAQy90ne2_XritzViMfKcWid6cdgh-Zz3PzfRx9LEyIPg"
        };
        var result = await _verifierServerClient.VerifyRevokeCodeAsync(input);
        result.ShouldBe(false);
    }
}