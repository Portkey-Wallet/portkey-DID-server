using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using CAServer.AppleVerify;
using CAServer.UserExtraInfo.Dtos;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CAServer.UserExtraInfo;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class UserExtraInfoTest : CAServerApplicationTestBase
{
    private readonly IUserExtraInfoAppService _userExtraInfoAppService;
    private JwtSecurityTokenHandler _jwtSecurityTokenHandler;

    public UserExtraInfoTest()
    {
        _userExtraInfoAppService = GetRequiredService<IUserExtraInfoAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockAppleUserProvider());
        services.AddSingleton(GetJwtSecurityTokenHandlerMock());
        services.AddSingleton(GetHttpClientService());
        // _jwtSecurityTokenHandler = Substitute.For<JwtSecurityTokenHandler>();
        // services.AddSingleton(_jwtSecurityTokenHandler);
    }

    private void Valid(string token)
    {
        _jwtSecurityTokenHandler.ReadJwtToken(token).Returns(new JwtSecurityToken(token));
        //_jwtSecurityTokenHandler.ValidateToken(token, It.IsAny<TokenValidationParameters>(), out var token1);
    }

    [Fact]
    public async Task AddAppleUserExtraInfo_Test()
    {
        var info = await _userExtraInfoAppService.AddAppleUserExtraInfoAsync(new AddAppleUserExtraInfoDto
        {
            IdentityToken =
                "eyJraWQiOiJXNldjT0tCIiwiYWxnIjoiUlMyNTYifQ.eyJpc3MiOiJodHRwczovL2FwcGxlaWQuYXBwbGUuY29tIiwiYXVkIjoiY29tLnBvcnRrZXkuZGlkIiwiZXhwIjoxNjc5NDcyMjg1LCJpYXQiOjE2NzkzODU4ODUsInN1YiI6IjAwMDMwMy5jZDgxN2I2OTgzMDc0ZDhjOGZiNzkyNDk2ZjI3N2ViYy4wMjU3IiwiY19oYXNoIjoicFBSeFFTSWNWY19BTEExSE9vdmJ5QSIsImVtYWlsIjoicHQ2eXhtOXptbUBwcml2YXRlcmVsYXkuYXBwbGVpZC5jb20iLCJlbWFpbF92ZXJpZmllZCI6InRydWUiLCJpc19wcml2YXRlX2VtYWlsIjoidHJ1ZSIsImF1dGhfdGltZSI6MTY3OTM4NTg4NSwibm9uY2Vfc3VwcG9ydGVkIjp0cnVlfQ.wXHXNbQVqvRxK_a6dq3WjBbJe_KaGsRVgSz_i3E01JyKW8rxGRRgDqjYNiTxB6iOqBMfvXfjtjgPl1N-de_Q4OflzG7gKK_17c-sY2uXUbOWVtAFI9WEXksYhZdV66eJDiUKJ8KE94S6NCT8UdkRqtxHtCnjuq82taYPbqcb-NO3Xcu23hfKsYQM_73yHJfnFd7jUYCoLHcxlVUeRGR7D7L3Yo9FdbocHZwei_x_jwb_7gYjTqGKg6rYt4MRT5ElSTj4xajXrRLZZzCFTVPjytvUsGvU038SEj4sIK6eoDAQy90ne2_XritzViMfKcWid6cdgh-Zz3PzfRx9LEyIPg",
            // "eyJhbGciOiJSUzI1NiIsImtpZCI6Ijg1MjA1QUI1NjRDNThEOTRCMzk3ODVEMDU3NjUxNUFCNDY2RTk1ODEiLCJ4NXQiOiJoU0JhdFdURmpaU3psNFhRVjJVVnEwWnVsWUUiLCJ0eXAiOiJhdCtqd3QifQ.eyJzdWIiOiJiMzQwNTIwOS1lYmE5LTRlMDYtYmMxOS0yZjAzMGQwMDljOWYiLCJvaV9wcnN0IjoiQ0FTZXJ2ZXJfQXBwIiwiY2xpZW50X2lkIjoiQ0FTZXJ2ZXJfQXBwIiwib2lfdGtuX2lkIjoiYTg4ZWFiMDAtYzhmZi05NDY3LTU1ODEtM2EwYTQ4ODdkZjU3IiwiYXVkIjoiQ0FTZXJ2ZXIiLCJzY29wZSI6IkNBU2VydmVyIiwianRpIjoiZDdlY2ZjOTktMGUzMy00NzZiLWJkOTYtNGQ1OWQzY2UyZDViIiwiZXhwIjoxNjgwNDE2OTQ3LCJpc3MiOiJodHRwOi8vMTcyLjMxLjMyLjIwNzo4MDgwLyIsImlhdCI6MTY4MDI0NDE0OH0.IP1JK_HJWow8HocVBF1nVEEDZk5jRoElju4mJzFZLZOSHiZdOK7ABEJmS4UAYG4it1Kpwa7YPD69veleQoihDmUSJ_ooBKrBb3ecE22JDiYStufaJuEM7PWisw06-XRQ9YLQI8pBnpXJHAaBfTCGp-cl-P9qxufRTZF9i-n_euQahlbDnLe0nDRjHS6aHV1-OlT8shoQXlNQDS0uf18DyIcqrqk0qkm5FQIwv0Q3mVZVTDuTxnR52-JXblTRbq8jR4NEaN9KHwHyFDEkvW-LytcBdiX-V-iMdH6KUtAJMLsztocK2HF7vHJqsLxaawhVXXeRseJlQJzLb_Ul3hLyQA",
            UserInfo = new AppleUserInfoDto
            {
                Name = new AppleUserName
                {
                    FirstName = "test_first_name",
                    LastName = "test_last_name"
                },
                Email = "test@qq.com",
            }
        });
        info.UserId.ShouldBe("123");

        var extraInfo = await _userExtraInfoAppService.GetUserExtraInfoAsync("123");
        extraInfo.Email.ShouldBe("test@qq.com");
    }
}