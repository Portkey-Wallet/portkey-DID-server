using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace CAServer.ThirdPart;

public static class TransakHelper
{
    private static readonly Dictionary<string, OrderStatusType> OrderStatusDict = new()
    {
        { "COMPLETED", OrderStatusType.Finish },
        { "FAILED", OrderStatusType.Failed },
        { "PROCESSING", OrderStatusType.Pending },
        { "AWAITING_PAYMENT_FROM_USER", OrderStatusType.Created },
        { "ON_HOLD_PENDING_DELIVERY_FROM_TRANSAK", OrderStatusType.UserCompletesCoinDeposit },
        { "PAYMENT_DONE_MARKED_BY_USER", OrderStatusType.StartPayment },
        { "PENDING_DELIVERY_FROM_TRANSAK", OrderStatusType.SuccessfulPayment },
        { "EXPIRED", OrderStatusType.Expired },
        { "CANCELLED", OrderStatusType.Canceled },
    };

    public static bool OrderStatusExist(string orderStatus)
    {
        return GetOrderStatus(orderStatus) != OrderStatusType.Unknown;
    }

    public static OrderStatusType GetOrderStatus(string status)
    {
        if (OrderStatusDict.TryGetValue(status, out OrderStatusType _))
        {
            return OrderStatusDict[status];
        }
        return OrderStatusType.Unknown;
    }
    
    public static string DecodeJwt(string data, string secretKey)
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

        tokenHandler.ValidateToken(data, tokenValidationParameters, out var validatedToken);
        var jwtToken = (JwtSecurityToken)validatedToken;
        return JsonConvert.SerializeObject(jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value));
    }
    
    public static string EncodeJwt(Dictionary<string, string> data, string secretKey)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(secretKey);

        var claims = new List<Claim>();
        foreach(var item in data)
            claims.Add(new Claim(item.Key, item.Value));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}