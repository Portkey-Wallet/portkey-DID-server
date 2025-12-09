using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CAServer.Growth.Dtos;
using Newtonsoft.Json;

namespace CAServer.Commons;

public class HMACSHA256Helper
{
    
    // detail for: https://tongifts.notion.site/TonGIfts-API-5b10982488de4662a31500e8815a7b4e
    public static string GenerateSignature(TonGiftsRequestDto param, string apiKey)
    {
        var paramMap = new Dictionary<string, object>()
        {
            { "status", param.Status },
            { "userIds", JsonConvert.SerializeObject(param.UserIds) },
            { "taskId", param.TaskId },
            { "t", param.T }
        };
        return GenerateSignature(paramMap, apiKey);
    }

    public static string GenerateSignature(Dictionary<string, object> parameters, string apiKey)
    
    {
        // Exclude 'k' and 's' keys
        var keys = parameters.Keys.Where(key => key != "k" && key != "s").ToList();
        var signData = new List<string>();

        foreach (var key in keys)
        {
            signData.Add($"{key}={parameters[key]}");
        }

        // Sort the data and create the raw string
        var rawStr = string.Join("&", signData.OrderBy(s => s));

        // Compute HMAC SHA256
        var selfHash = HMAC_SHA256(rawStr, apiKey);
        
        return selfHash;
    }

    private static string HMAC_SHA256(string data, string key)
    {
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
        {
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
    
}