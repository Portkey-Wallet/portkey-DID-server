using System;
using System.Text;
using Google.Apis.Json;

namespace CAServer.Google.Utils;

public class JwtUtils
{
    
    private const string AlgorithmRS256 = "RS256";
    
    public static T Decode<T>(string value) => NewtonsoftJsonSerializer.Instance.Deserialize<T>(JwtUtils.Base64Decode(value));
    
    public static string Base64Decode(string input) => Encoding.UTF8.GetString(JwtUtils.Base64DecodeToBytes(input));
    
    public static byte[] Base64DecodeToBytes(string input)
    {
        input = input.Replace('-', '+').Replace('_', '/');
        switch (input.Length % 4)
        {
            case 2:
                input += "==";
                break;
            case 3:
                input += "=";
                break;
        }
        return Convert.FromBase64String(input);
    }

    
}