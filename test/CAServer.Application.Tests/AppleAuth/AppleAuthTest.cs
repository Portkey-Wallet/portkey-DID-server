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
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Types;
using CAServer.AppleAuth.Dtos;
using CAServer.AppleVerify;
using CAServer.Commons;
using CAServer.Growth.Dtos;
using CAServer.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Portkey.Contracts.CA;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace CAServer.AppleAuth;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class AppleAuthTest : CAServerApplicationTestBase
{
    private readonly IAppleAuthAppService _appleAuthAppService;
    private readonly ITestOutputHelper _testOutputHelper;

    public AppleAuthTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _appleAuthAppService = GetService<IAppleAuthAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetJwtSecurityTokenHandlerMock());
        services.AddSingleton(GetMockAppleUserProvider());
        services.AddSingleton(GetMockAppleAuthOptions());
        services.AddSingleton(GetMockHttpClientFactory());
    }

    [Fact]
    public async Task DecodeRawTransactionTest()
    {

        var v1 = new Version("1.30.0");
        var v2 = new Version("1.21.0");
        var result = v1 > v2;


        var publicKey =
            "04bc680e9f8ea189fb510f3f9758587731a9a64864f9edbc706cea6e8bf85cf6e56f236ba58d8840f3fce34cbf16a97f69dc784183d2eef770b367f6e8a90151af";
        var rawTransaction =
            "0a220a20e53eff822ad4b33e8ed0356a55e5b8ea83a88afdb15bdedcf52646d8c13209c812220a20e28c0b6c4145f3534431326f3c6d5a4bd6006632fd7551c26c103c368855531618d78ad30c2204ec4557c32a124d616e61676572466f727761726443616c6c3285010a220a20ffc98c7be1a50ada7ca839da2ecd94834525bdcea392792957cc7f1b2a0c3a1e12220a202791e992a57f28e75a11f13af2c0aec8b0eb35d2f048d42eba8901c92e0378dc1a085472616e7366657222310a220a207d2fbc18d14225b1708a15841d450973c12ae789c9e1f86c9c31a8cc3f86f5171203454c461880d88ee16f220082f1044198f955dcda3b805e944b3611f78144bab8a3a33d2ed7480cc5fbc325eda8efa27185755791e64f2682f00ea26b9507443453fe44059fe8e58f6a122b22f3474e00";
        var transaction = Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(rawTransaction));
        _testOutputHelper.WriteLine(JsonConvert.SerializeObject(transaction));
        _testOutputHelper.WriteLine("VerifySignature : " + VerifyHelper.VerifySignature(transaction, publicKey));

        var param = ManagerForwardCallInput.Parser.ParseFrom(transaction.Params);
        _testOutputHelper.WriteLine(JsonConvert.SerializeObject(param));
        var transferArgs = TransferInput.Parser.ParseFrom(param.Args);
        _testOutputHelper.WriteLine(JsonConvert.SerializeObject(transferArgs));
    }

    [Fact]
    public async Task ReceiveTest()
    {
        try
        {
            await _appleAuthAppService.ReceiveAsync(new AppleAuthDto()
            {
                Code = "",
                Id_token =
                    "eyJraWQiOiJXNldjT0tCIiwiYWxnIjoiUlMyNTYifQ.eyJpc3MiOiJodHRwczovL2FwcGxlaWQuYXBwbGUuY29tIiwiYXVkIjoiY29tLnBvcnRrZXkuZGlkIiwiZXhwIjoxNjc5NDcyMjg1LCJpYXQiOjE2NzkzODU4ODUsInN1YiI6IjAwMDMwMy5jZDgxN2I2OTgzMDc0ZDhjOGZiNzkyNDk2ZjI3N2ViYy4wMjU3IiwiY19oYXNoIjoicFBSeFFTSWNWY19BTEExSE9vdmJ5QSIsImVtYWlsIjoicHQ2eXhtOXptbUBwcml2YXRlcmVsYXkuYXBwbGVpZC5jb20iLCJlbWFpbF92ZXJpZmllZCI6InRydWUiLCJpc19wcml2YXRlX2VtYWlsIjoidHJ1ZSIsImF1dGhfdGltZSI6MTY3OTM4NTg4NSwibm9uY2Vfc3VwcG9ydGVkIjp0cnVlfQ.wXHXNbQVqvRxK_a6dq3WjBbJe_KaGsRVgSz_i3E01JyKW8rxGRRgDqjYNiTxB6iOqBMfvXfjtjgPl1N-de_Q4OflzG7gKK_17c-sY2uXUbOWVtAFI9WEXksYhZdV66eJDiUKJ8KE94S6NCT8UdkRqtxHtCnjuq82taYPbqcb-NO3Xcu23hfKsYQM_73yHJfnFd7jUYCoLHcxlVUeRGR7D7L3Yo9FdbocHZwei_x_jwb_7gYjTqGKg6rYt4MRT5ElSTj4xajXrRLZZzCFTVPjytvUsGvU038SEj4sIK6eoDAQy90ne2_XritzViMfKcWid6cdgh-Zz3PzfRx9LEyIPg",
                User = JsonConvert.SerializeObject(new AppleExtraInfo
                {
                    Name = new AppleNameInfo()
                    {
                        FirstName = "Li",
                        LastName = "Ning"
                    },
                    Email = "test@qq.com"
                })
            });
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task Receive_Code_Test()
    {
        try
        {
            await _appleAuthAppService.ReceiveAsync(new AppleAuthDto()
            {
                Code = "test",
                Id_token = string.Empty,
                User = JsonConvert.SerializeObject(new AppleExtraInfo
                {
                    Name = new AppleNameInfo()
                    {
                        FirstName = "Li",
                        LastName = "Ning"
                    },
                    Email = "test@qq.com"
                })
            });
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task Receive_Invalid_Params_Test()
    {
        try
        {
            var options = new AppleAuthOptions
            {
                Audiences = new List<string>(),
                RedirectUrl = string.Empty,
                BingoRedirectUrl = string.Empty,
                UnifyRedirectUrl = string.Empty,
                ExtensionConfig = new ExtensionConfig
                {
                    TeamId = string.Empty,
                    ClientId = string.Empty,
                    KeyId = string.Empty
                }
            };

            await _appleAuthAppService.ReceiveAsync(new AppleAuthDto()
            {
                Code = string.Empty,
                Id_token = string.Empty
            });
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
            e.Message.ShouldContain("valid");
        }
    }

    private JwtSecurityTokenHandler GetJwtSecurityTokenHandlerMock()
    {
        var jwtSecurityTokenHandler = new Mock<JwtSecurityTokenHandler>();
        var jToken = new JwtSecurityToken
        {
            Payload = { { "email_verified", "true" }, { "is_private_email", "false" } },
        };
        jToken.Payload.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, "123"));
        SecurityToken token = jToken;

        jwtSecurityTokenHandler.Setup(p => p.ValidateToken(It.IsAny<string>(),
                It.IsAny<TokenValidationParameters>(),
                out token))
            .Returns(SelectClaimsPrincipal());
        jwtSecurityTokenHandler.Setup(p => p.MaximumTokenSizeInBytes).Returns(1000000);
        jwtSecurityTokenHandler.Setup(p => p.CanReadToken(It.IsAny<string>())).Returns(true);

        jwtSecurityTokenHandler.Setup(t => t.WriteToken(It.IsAny<SecurityToken>()))
            .Returns("secret");

        return jwtSecurityTokenHandler.Object;
    }

    private static ClaimsPrincipal SelectClaimsPrincipal()
    {
        IPrincipal currentPrincipal = Thread.CurrentPrincipal;
        return currentPrincipal is ClaimsPrincipal claimsPrincipal
            ? claimsPrincipal
            : (currentPrincipal == null ? (ClaimsPrincipal)null : new ClaimsPrincipal(currentPrincipal));
    }

    private IOptions<AppleAuthOptions> GetMockAppleAuthOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptions<AppleAuthOptions>>();

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
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Get &&
                    r.RequestUri.ToString().Equals("https://appleid.apple.com/auth/keys")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(appleKeys)),
            });

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri.ToString().Equals("https://appleid.apple.com/auth/token")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    id_token =
                        "eyJraWQiOiJXNldjT0tCIiwiYWxnIjoiUlMyNTYifQ.eyJpc3MiOiJodHRwczovL2FwcGxlaWQuYXBwbGUuY29tIiwiYXVkIjoiY29tLnBvcnRrZXkuZGlkIiwiZXhwIjoxNjc5NDcyMjg1LCJpYXQiOjE2NzkzODU4ODUsInN1YiI6IjAwMDMwMy5jZDgxN2I2OTgzMDc0ZDhjOGZiNzkyNDk2ZjI3N2ViYy4wMjU3IiwiY19oYXNoIjoicFBSeFFTSWNWY19BTEExSE9vdmJ5QSIsImVtYWlsIjoicHQ2eXhtOXptbUBwcml2YXRlcmVsYXkuYXBwbGVpZC5jb20iLCJlbWFpbF92ZXJpZmllZCI6InRydWUiLCJpc19wcml2YXRlX2VtYWlsIjoidHJ1ZSIsImF1dGhfdGltZSI6MTY3OTM4NTg4NSwibm9uY2Vfc3VwcG9ydGVkIjp0cnVlfQ.wXHXNbQVqvRxK_a6dq3WjBbJe_KaGsRVgSz_i3E01JyKW8rxGRRgDqjYNiTxB6iOqBMfvXfjtjgPl1N-de_Q4OflzG7gKK_17c-sY2uXUbOWVtAFI9WEXksYhZdV66eJDiUKJ8KE94S6NCT8UdkRqtxHtCnjuq82taYPbqcb-NO3Xcu23hfKsYQM_73yHJfnFd7jUYCoLHcxlVUeRGR7D7L3Yo9FdbocHZwei_x_jwb_7gYjTqGKg6rYt4MRT5ElSTj4xajXrRLZZzCFTVPjytvUsGvU038SEj4sIK6eoDAQy90ne2_XritzViMfKcWid6cdgh-Zz3PzfRx9LEyIPg",
                    other = "test"
                })),
            });

        var client = new HttpClient(mockHttpMessageHandler.Object);
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);
        var factory = mockFactory.Object;
        return factory;
    }
}