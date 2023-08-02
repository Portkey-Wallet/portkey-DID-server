using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace CAServer.ThirdPart;

public class TransakTest : CAServerApplicationTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public TransakTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void DecodeJwtTest()
    {
        var data = "eyJhbGciOiJIUzI1NiJ9.eyJ3ZWJob29rRGF0YSI6eyJpZCI6IjkxNTFmYWExLWU2OWItNGEzNi1iOTU5LTNjNGY4OTRhZmI2OCIsIndhbGxldEFkZHJlc3MiOiIweDg2MzQ5MDIwZTkzOTRiMkJFMWIxMjYyNTMxQjBDMzMzNWZjMzJGMjAiLCJjcmVhdGVkQXQiOiIyMDIwLTAyLTE3VDAxOjU1OjA1LjA5NVoiLCJzdGF0dXMiOiJBV0FJVElOR19QQVlNRU5UX0ZST01fVVNFUiIsImZpYXRDdXJyZW5jeSI6IklOUiIsInVzZXJJZCI6IjY1MzE3MTMxLWNkOTUtNDE5YS1hNTBjLTc0N2QxNDJmODNlOSIsImNyeXB0b0N1cnJlbmN5IjoiQ0RBSSIsImlzQnV5T3JTZWxsIjoiQlVZIiwiZmlhdEFtb3VudCI6MTExMCwiZnJvbVdhbGxldEFkZHJlc3MiOiIweDA4NWVlNjcxMzJlYzQyOTdiODVlZDVkMWI0YzY1NDI0ZDM2ZmRhN2QiLCJ3YWxsZXRMaW5rIjoiaHR0cHM6Ly9yaW5rZWJ5LmV0aGVyc2Nhbi5pby9hZGRyZXNzLzB4ODYzNDkwMjBlOTM5NGIyQkUxYjEyNjI1MzFCMEMzMzM1ZmMzMkYyMCN0b2tlbnR4bnMiLCJhbW91bnRQYWlkIjowLCJwYXJ0bmVyT3JkZXJJZCI6IjIxODM3MjE4OTMiLCJwYXJ0bmVyQ3VzdG9tZXJJZCI6IjIxODM3MjE4OTMiLCJyZWRpcmVjdFVSTCI6Imh0dHBzOi8vZ29vZ2xlLmNvbSIsImNvbnZlcnNpb25QcmljZSI6MC42NjM4NDcxNjQzNjg2MDYsImNyeXB0b0Ftb3VudCI6NzMxLjM0LCJ0b3RhbEZlZSI6NS41MjY1Mjc2NDMzNjg2NCwiYXV0b0V4cGlyZXNBdCI6IjIwMjAtMDItMTZUMTk6NTU6MDUtMDc6MDAiLCJyZWZlcmVuY2VDb2RlIjoyMjYwNTZ9LCJldmVudElEIjoiT1JERVJfQ1JFQVRFRCIsImNyZWF0ZWRBdCI6IjIwMjMtMDgtMDJUMDc6MDI6NTMuMjQ5WiJ9.61L0In4DXAuGhkIKthGObdph47kCSyhtoecOkTAIWcE";
        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJBUElfS0VZIjoiMDljMDU2ZmQtZDQyMy00NmQ5LWE2NDEtZTRhN2ExZTdkZTMzIiwiaWF0IjoxNjkwOTU1OTQxLCJleHAiOjE2OTE1NjA3NDF9.j3mn6ctBKPnkkiYRchg-BzGgdI9ZfUgH3bbC0QIGtkM";
        _testOutputHelper.WriteLine(DecodeJwt(data, token));
    }
    
    
    public string DecodeJwt(string token, string secretKey)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(secretKey);

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
        };

        tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
        var jwtToken = (JwtSecurityToken)validatedToken;
        return JsonConvert.SerializeObject(jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value));
    }
}