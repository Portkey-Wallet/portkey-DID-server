using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CAServer.Telegram.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp.DependencyInjection;

namespace CAServer.Telegram.Provider;

public interface IJwtTokenProvider
{
    Task<string> GenerateTokenAsync(IDictionary<string, string> userInfo);
}

public class JwtTokenProvider : IJwtTokenProvider, ISingletonDependency
{
    private readonly ILogger<JwtTokenProvider> _logger;
    private readonly JwtTokenOptions _jwtTokenOptions;

    public JwtTokenProvider(ILogger<JwtTokenProvider> logger, IOptionsSnapshot<JwtTokenOptions> jwtTokenOptions)
    {
        _logger = logger;
        _jwtTokenOptions = jwtTokenOptions.Value;
    }

    public async Task<string> GenerateTokenAsync(IDictionary<string, string> userInfo)
    {
        var privateKey = Convert.FromBase64String(_jwtTokenOptions.PrivateKey);
        using (var rsa = new RSACryptoServiceProvider())
        {
            rsa.ImportPkcs8PrivateKey(privateKey, out _);
            AsymmetricSecurityKey asymmetricSecurityKey = new RsaSecurityKey(rsa.ExportParameters(true));
            var signingCredentials = new SigningCredentials(asymmetricSecurityKey, SecurityAlgorithms.RsaSha256);

            var claims = new Claim[userInfo.Count];
            int index = 0;
            foreach (var keyValuePair in userInfo)
            {
                claims[index] = new Claim(keyValuePair.Key, keyValuePair.Value);
                index++;
            }

            var token = new JwtSecurityToken(
                issuer: _jwtTokenOptions.Issuer,
                audience: _jwtTokenOptions.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddSeconds(_jwtTokenOptions.expire),
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}