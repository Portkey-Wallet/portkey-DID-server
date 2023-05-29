using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using CAServer.AppleAuth.Dtos;
using CAServer.AppleAuth.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace CAServer.AppleAuth;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class AppleAuthTest : CAServerApplicationTestBase
{
    private readonly IAppleAuthAppService _appleAuthAppService;

    public AppleAuthTest()
    {
        _appleAuthAppService = GetService<IAppleAuthAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetJwtSecurityTokenHandlerMock());
        services.AddSingleton(GetMockAppleUserProvider());
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
    //todo mock client and get token

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
        return jwtSecurityTokenHandler.Object;
    }

    private static ClaimsPrincipal SelectClaimsPrincipal()
    {
        IPrincipal currentPrincipal = Thread.CurrentPrincipal;
        return currentPrincipal is ClaimsPrincipal claimsPrincipal
            ? claimsPrincipal
            : (currentPrincipal == null ? (ClaimsPrincipal)null : new ClaimsPrincipal(currentPrincipal));
    }
}