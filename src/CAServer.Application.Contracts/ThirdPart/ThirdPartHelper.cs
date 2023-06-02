using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CAServer.ThirdPart.Dtos;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace CAServer.ThirdPart;

public static class ThirdPartHelper
{
    public static bool MerchantNameExist(string merchantName)
    {
        return Enum.TryParse(merchantName, out MerchantNameType _);
    }

    public static bool TransferDirectionTypeExist(string transferDirectionType)
    {
        return Enum.TryParse(transferDirectionType, out TransferDirectionType _);
    }

    public static bool ValidateMerchantOrderNo(string merchantOrderNo)
    {
        return merchantOrderNo.IsNullOrEmpty() || Guid.TryParse(merchantOrderNo, out Guid _);
    }

    public static Guid GetOrderId(string merchantOrderNo)
    {
        Guid.TryParse(merchantOrderNo, out Guid orderNo);
        return orderNo;
    }
}

public class AlchemyHelper
{
    private readonly ILogger<AlchemyHelper> _logger;
    
    public AlchemyHelper(ILogger<AlchemyHelper> logger)
    {
        _logger = logger;
    }

    private static Dictionary<string, OrderStatusType> _orderStatusDict = new()
    {
        { "FINISHED", OrderStatusType.Finish },
        { "PAY_FAIL", OrderStatusType.Failed },
        { "PAY_SUCCESS", OrderStatusType.Pending },
        { "1", OrderStatusType.Finish },
        { "2", OrderStatusType.Pending },
        { "3", OrderStatusType.Pending },
        { "4", OrderStatusType.Pending },
        { "5", OrderStatusType.Failed },
        { "6", OrderStatusType.Refunded },
        { "7", OrderStatusType.Expired },
    };

    public static bool OrderStatusExist(string orderStatus)
    {
        return GetOrderStatus(orderStatus) != OrderStatusType.Unknown.ToString();
    }

    public static string GetOrderStatus(string status)
    {
        if (_orderStatusDict.TryGetValue(status, out OrderStatusType _))
        {
            return _orderStatusDict[status].ToString();
        }

        return "Unknown";
    }
    
    // doc: https://alchemypay.readme.io/docs/api-sign
    public AlchemySignatureResultDto GetAlchemySignatureAsync(object input, string appSecret, List<string> ignoreProperties)
    {
        try
        {
            var signParamDictionary = ConvertObjectToDictionary(input);
            
            // ignore some key such as "signature" properties
            foreach (var key in ignoreProperties?? new List<string>())
            {
                signParamDictionary.Remove(key);
            }
            
            var sortedParams = signParamDictionary.OrderBy(d => d.Key, StringComparer.Ordinal);
            var signSource = string.Join("&", sortedParams.Select(kv => $"{kv.Key}={kv.Value}"));
            _logger.Debug("[ACH] signSource = {signSource}", signSource);
            return new AlchemySignatureResultDto()
            {
                Signature = ComputeHmacSha256(signSource, appSecret)
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "AES encrypting exception");
            return new AlchemySignatureResultDto()
            {
                Success = "Fail",
                ReturnMsg = $"Error AES encrypting, error msg is {e.Message}"
            };
        }
    }

    private static Dictionary<string, string> ConvertObjectToDictionary(object obj)
    {
        var dict = new Dictionary<string, string>();
        if (obj == null) return dict;
        
        var emptyGuid = new Guid();

        // If the object is a dictionary, handle it separately
        if (obj is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                dict.Add(entry.Key.ToString() ?? string.Empty, entry.Value?.ToString());
            }

            return dict;
        }

        // If not, process each property
        foreach (var property in obj.GetType().GetProperties())
        {
            // Skip indexed properties
            if (property.GetIndexParameters().Length != 0) continue;
            if (property.PropertyType != typeof(string) && !property.PropertyType.IsValueType) continue;

            var value = property.GetValue(obj);

            // Skip null value or empty Guid value
            if (value == null || property.PropertyType == typeof(Guid) && value.Equals(emptyGuid)) continue;

            // convert first char to lower case 
            dict.Add(property.Name.Substring(0, 1).ToLowerInvariant() + property.Name.Substring(1),
                value.ToString());
        }

        return dict;
    }
    
    public static string AesEncrypt(string plainText, string secretKeyData)
    {
        try
        {
            byte[] plainTextData = Encoding.UTF8.GetBytes(plainText);
            byte[] secretKey = Encoding.UTF8.GetBytes(secretKeyData);
            string iv = Encoding.UTF8.GetString(secretKey).Substring(0, 16);

            AesManaged aesAlgorithm = new AesManaged();
            aesAlgorithm.Mode = CipherMode.CBC;
            aesAlgorithm.Padding = PaddingMode.PKCS7;

            byte[] dataBytes = plainTextData;
            int plaintextLength = dataBytes.Length;
            byte[] plaintext = new byte[plaintextLength];
            Array.Copy(dataBytes, 0, plaintext, 0, dataBytes.Length);

            ICryptoTransform encryptor = aesAlgorithm.CreateEncryptor(secretKey, Encoding.UTF8.GetBytes(iv));

            byte[] encryptedData = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

            return Convert.ToBase64String(encryptedData);
        }
        catch (Exception e)
        {
            throw e;
        }
    }

    private static string ComputeHmacSha256(string message, string secretKey)
    {
        var encoding = new ASCIIEncoding();
        var keyByte = encoding.GetBytes(secretKey);
        var messageBytes = encoding.GetBytes(message);
        using var hmacSha256 = new HMACSHA256(keyByte);
        var hashMessage = hmacSha256.ComputeHash(messageBytes);
        return BitConverter.ToString(hashMessage).Replace("-", "").ToLower();
    }
}