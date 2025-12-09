using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using CAServer.AppleAuth.Provider;
using CAServer.AppleVerify;
using CAServer.Signature.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace CAServer.AppleAuth;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class AppleAuthProviderTest : CAServerApplicationTestBase
{
    private readonly IAppleAuthProvider _appleAuthProvider;

    public AppleAuthProviderTest()
    {
        _appleAuthProvider = GetRequiredService<IAppleAuthProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetJwtSecurityTokenHandlerMock());
        services.AddSingleton(GetMockHttpClientFactory());
        services.AddSingleton(GetMockAppleAuthOptions());
        services.AddSingleton(MockSecretProvider());
        // services.AddSingleton(GetMockECDsaSecurityKey());
        // services.AddSingleton(GetECDsaSecurityKeyMock());
    }

    [Fact]
    public async Task VerifyAppleIdTest()
    {
        var identityToken =
            "eyJraWQiOiJXNldjT0tCIiwiYWxnIjoiUlMyNTYifQ.eyJpc3MiOiJodHRwczovL2FwcGxlaWQuYXBwbGUuY29tIiwiYXVkIjoiY29tLnBvcnRrZXkuZGlkIiwiZXhwIjoxNjc5NDcyMjg1LCJpYXQiOjE2NzkzODU4ODUsInN1YiI6IjAwMDMwMy5jZDgxN2I2OTgzMDc0ZDhjOGZiNzkyNDk2ZjI3N2ViYy4wMjU3IiwiY19oYXNoIjoicFBSeFFTSWNWY19BTEExSE9vdmJ5QSIsImVtYWlsIjoicHQ2eXhtOXptbUBwcml2YXRlcmVsYXkuYXBwbGVpZC5jb20iLCJlbWFpbF92ZXJpZmllZCI6InRydWUiLCJpc19wcml2YXRlX2VtYWlsIjoidHJ1ZSIsImF1dGhfdGltZSI6MTY3OTM4NTg4NSwibm9uY2Vfc3VwcG9ydGVkIjp0cnVlfQ.wXHXNbQVqvRxK_a6dq3WjBbJe_KaGsRVgSz_i3E01JyKW8rxGRRgDqjYNiTxB6iOqBMfvXfjtjgPl1N-de_Q4OflzG7gKK_17c-sY2uXUbOWVtAFI9WEXksYhZdV66eJDiUKJ8KE94S6NCT8UdkRqtxHtCnjuq82taYPbqcb-NO3Xcu23hfKsYQM_73yHJfnFd7jUYCoLHcxlVUeRGR7D7L3Yo9FdbocHZwei_x_jwb_7gYjTqGKg6rYt4MRT5ElSTj4xajXrRLZZzCFTVPjytvUsGvU038SEj4sIK6eoDAQy90ne2_XritzViMfKcWid6cdgh-Zz3PzfRx9LEyIPg";
        var appleId = "123";
        await _appleAuthProvider.VerifyAppleId(identityToken, appleId);
    }

    [Fact]
    public async Task RevokeTest()
    {
        var jToken = GetJwtSecurityToken();
        var revokeResult = await _appleAuthProvider.RevokeAsync(jToken.ToString());
        revokeResult.ShouldBeTrue();
    }

    private JwtSecurityTokenHandler GetJwtSecurityTokenHandlerMock()
    {
        var jwtSecurityTokenHandler = new Mock<JwtSecurityTokenHandler>();
        var jToken = GetJwtSecurityToken();
        SecurityToken token = jToken;

        jwtSecurityTokenHandler.Setup(p => p.ValidateToken(It.IsAny<string>(),
                It.IsAny<TokenValidationParameters>(),
                out token))
            .Returns(SelectClaimsPrincipal());
        jwtSecurityTokenHandler.Setup(p => p.MaximumTokenSizeInBytes).Returns(1000000);
        jwtSecurityTokenHandler.Setup(p => p.CanReadToken(It.IsAny<string>())).Returns(true);

        jwtSecurityTokenHandler.Setup(t => t.CreateJwtSecurityToken(It.IsAny<SecurityTokenDescriptor>()))
            .Returns(jToken);

        jwtSecurityTokenHandler.Setup(t => t.WriteToken(It.IsAny<SecurityToken>()))
            .Returns("secret");

        return jwtSecurityTokenHandler.Object;
    }

    private JwtSecurityToken GetJwtSecurityToken()
    {
        var jToken = new JwtSecurityToken
        {
            Header = { { "kid", "test" } },
            Payload = { { "email_verified", "true" }, { "is_private_email", "false" } }
        };

        jToken.Payload.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, "123"));
        return jToken;
    }

    private static ClaimsPrincipal SelectClaimsPrincipal()
    {
        IPrincipal currentPrincipal = Thread.CurrentPrincipal;
        return currentPrincipal is ClaimsPrincipal claimsPrincipal
            ? claimsPrincipal
            : (currentPrincipal == null ? (ClaimsPrincipal)null : new ClaimsPrincipal(currentPrincipal));
    }

    private IHttpClientFactory GetMockHttpClientFactory()
    {
        var appleKeys = new AppleKeys()
        {
            Keys = new List<AppleKey>()
            {
                new()
                {
                    Kid = "W6WcOKB",
                    Alg = "RS256"
                }
            }
        };

        var mockFactory = new Mock<IHttpClientFactory>();
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(appleKeys)),
            });

        var client = new HttpClient(mockHttpMessageHandler.Object);
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);
        var factory = mockFactory.Object;
        return factory;
    }

    private IOptionsSnapshot<AppleAuthOptions> GetMockAppleAuthOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<AppleAuthOptions>>();

        var pkcs8PrivateKeyBytes = ECDsa.Create().ExportPkcs8PrivateKey();
        var pkcs8PrivateKey = Convert.ToBase64String(pkcs8PrivateKeyBytes);

        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new AppleAuthOptions
            {
                ExtensionConfig = new ExtensionConfig()
                {
                    ClientId = "test",
                    KeyId = "test",
                    TeamId = "test"
                }
            });
        return mockOptionsSnapshot.Object;
    }

    private ECDsaSecurityKey GetECDsaSecurityKeyMock()
    {
        var key = new Mock<ECDsaSecurityKey>();
        var byteRead = 1000;
        key.Setup(t => t.ECDsa.ImportPkcs8PrivateKey(Convert.FromBase64String("test"), out byteRead));
        return key.Object;
    }

    private ECAlgorithm GetMockECDsaSecurityKey()
    {
        var secKey = new Mock<ECAlgorithm>();
        var byteRead = 1000;
        secKey.Setup(t => t.ImportPkcs8PrivateKey(Convert.FromBase64String("test"), out byteRead));
        return secKey.Object;
    }
}