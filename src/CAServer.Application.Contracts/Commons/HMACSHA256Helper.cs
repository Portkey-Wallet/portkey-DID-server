using System;
using System.Security.Cryptography;
using System.Text;

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
    
}