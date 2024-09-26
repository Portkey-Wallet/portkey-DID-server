using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace CAServer.Commons;

public class HMACSHA256Helper
{
    
    public static string ComputeHash(string data, string key)
    {
        var encoding = new UTF8Encoding();
        byte[] keyBytes = encoding.GetBytes(key);
        byte[] messageBytes = encoding.GetBytes(data);
        using (var hmacsha256 = new HMACSHA256(keyBytes))
        {
            byte[] hashBytes = hmacsha256.ComputeHash(messageBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
    
    // detail for: https://tongifts.notion.site/TonGIfts-API-5b10982488de4662a31500e8815a7b4e
    public static string GenerateSignature(Dictionary<string, object> parameters, string apiKey)
    {
        // Exclude 'k' and 's' keys
        var keys = parameters.Keys.Where(key => key != "k" && key != "s").ToList();
        var signData = new List<string>();

        foreach (var key in keys)
        {
            if (parameters[key] is Dictionary<string, object> obj)
            {
                signData.Add($"{key}={JsonConvert.SerializeObject(obj)}");
            }
            else
            {
                signData.Add($"{key}={parameters[key]}");
            }
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