using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using CAServer.AppleVerify;
using CAServer.Common;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NSubstitute;
using RabbitMQ.Client;

namespace CAServer.UserExtraInfo;

public partial class UserExtraInfoTest
{
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

    private IHttpClientService GetHttpClientService()
    {
        var mock = new Mock<IHttpClientService>();
        var kid = "W6WcOKB";
        // "85205AB564C58D94B39785D0576515AB466E9581"
        mock.Setup(p => p.GetAsync<AppleKeys>(It.IsAny<string>())).Returns(Task.FromResult(new AppleKeys
        {
            Keys = new List<AppleKey> { new() { Kid = kid } }
        }));
        return mock.Object;
    }

    private static ClaimsPrincipal SelectClaimsPrincipal()
    {
        IPrincipal currentPrincipal = Thread.CurrentPrincipal;
        return currentPrincipal is ClaimsPrincipal claimsPrincipal ? claimsPrincipal : (currentPrincipal == null ? (ClaimsPrincipal)null : new ClaimsPrincipal(currentPrincipal));
    }
}

public static class MockJwtTokens
{
    public static string Issuer { get; } = Guid.NewGuid().ToString();
    public static SecurityKey SecurityKey { get; }
    public static SigningCredentials SigningCredentials { get; }

    private static readonly JwtSecurityTokenHandler s_tokenHandler = new JwtSecurityTokenHandler();
    private static readonly RandomNumberGenerator s_rng = RandomNumberGenerator.Create();
    private static readonly byte[] s_key = new byte[32];

    static MockJwtTokens()
    {
        s_rng.GetBytes(s_key);
        SecurityKey = new SymmetricSecurityKey(s_key) { KeyId = Guid.NewGuid().ToString() };
        SigningCredentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
    }

    public static string GenerateJwtToken(IEnumerable<Claim> claims)
    {
        return s_tokenHandler.WriteToken(new JwtSecurityToken(Issuer, null, claims, null, DateTime.UtcNow.AddMinutes(20), SigningCredentials));
    }
}
