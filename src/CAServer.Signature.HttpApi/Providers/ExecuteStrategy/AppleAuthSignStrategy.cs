using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using SignatureServer.Common;
using SignatureServer.Dtos;

namespace SignatureServer.Providers.ExecuteStrategy;

public class AppleAuthStrategy : IThirdPartExecuteStrategy<AppleAuthExecuteInput, CommonThirdPartExecuteOutput>
{

    private readonly JwtSecurityTokenHandler _jwtSecurityTokenHandler;

    public AppleAuthStrategy(JwtSecurityTokenHandler jwtSecurityTokenHandler)
    {
        _jwtSecurityTokenHandler = jwtSecurityTokenHandler;
    }

    public ThirdPartExecuteStrategy ExecuteStrategy()
    {
        return ThirdPartExecuteStrategy.AppleAuth;
    }

    public CommonThirdPartExecuteOutput Execute(string secret, AppleAuthExecuteInput input)
    {
        
        var key = new ECDsaSecurityKey(ECDsa.Create());
        key.ECDsa.ImportPkcs8PrivateKey(Convert.FromBase64String(secret), out _);
        key.KeyId = input.KeyId;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = input.TeamId,
            Audience = "https://appleid.apple.com",
            Subject = new ClaimsIdentity(new[] { new Claim("sub", input.ClientId) }),
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(180),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256)
        };
        var token = _jwtSecurityTokenHandler.CreateJwtSecurityToken(descriptor);
        var clientSecret = _jwtSecurityTokenHandler.WriteToken(token);
        return new CommonThirdPartExecuteOutput(clientSecret);
    }
    
}