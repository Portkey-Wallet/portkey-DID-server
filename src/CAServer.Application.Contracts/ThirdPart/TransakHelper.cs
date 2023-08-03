using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using CAServer.ThirdPart.Dtos;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace CAServer.ThirdPart;

public static class TransakHelper
{
    private static Dictionary<string, OrderStatusType> _orderStatusDict = new()
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
        if (_orderStatusDict.TryGetValue(status, out OrderStatusType _))
        {
            return _orderStatusDict[status];
        }
        return OrderStatusType.Unknown;
    }
    
    public static string DecodeJwt(string token, string secretKey)
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