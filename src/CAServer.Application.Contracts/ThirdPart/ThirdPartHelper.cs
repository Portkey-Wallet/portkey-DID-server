using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;

namespace CAServer.ThirdPart;

public static class ThirdPartHelper
{
    public static ThirdPartNameType MerchantNameExist(string merchantName)
    {
        var match = Enum.TryParse(merchantName, out ThirdPartNameType val);
        return match && Enum.IsDefined(typeof(ThirdPartNameType), val) ? val : ThirdPartNameType.Unknown;
    }

    public static OrderStatusType ParseOrderStatus(string statusStr)
    {
        var match = Enum.TryParse(statusStr, out OrderStatusType val);
        return match && Enum.IsDefined(typeof(OrderStatusType), val)  ? val : OrderStatusType.Unknown;
    }

    public static bool TransferDirectionTypeExist(string transferDirectionType)
    {
        var match = Enum.TryParse(transferDirectionType, out TransferDirectionType val);
        return match && Enum.IsDefined(typeof(TransferDirectionType), val);
    }

    public static bool ValidateMerchantOrderNo(string merchantOrderNo)
    {
        return merchantOrderNo.IsNullOrEmpty() || Guid.TryParse(merchantOrderNo, out Guid _);
    }
    
    public static Guid GenerateOrderId(string merchantName, string merchantOrderNo)
    {
        return new Guid(MD5.HashData(Encoding.Default.GetBytes(merchantName + merchantOrderNo)));
    }

    public static Guid GetOrderId(string merchantOrderNo)
    {
        Guid.TryParse(merchantOrderNo, out Guid orderNo);
        return orderNo;
    }
 
    public static string ConvertObjectToSortedString([CanBeNull] object obj, params string[] ignoreParams)
    {
        if (obj == null) return string.Empty;
        var dict = new SortedDictionary<string, object>();

        if (obj is IDictionary<string, string> inputDict)
        {
            foreach (var kvp in inputDict)
            {
                if (ignoreParams.Contains(kvp.Key)) continue;
                if (kvp.Value == null) continue; // ignore null value
                dict[kvp.Key] = kvp.Value;
            }
        }
        else
        {
            foreach (var property in obj.GetType().GetProperties())
            {
                var key = property.Name.Substring(0, 1).ToLower() + property.Name.Substring(1);
                if (!property.CanRead || ignoreParams.Contains(key)) continue;

                var value = property.GetValue(obj);
                if (value == null) continue; // ignore null value

                dict[key] = value;
            }
        }

        return string.Join("&", dict.Select(kv => kv.Key + "=" + kv.Value));
    }
}

public static class /**/AlchemyHelper
{
    public const string SignatureField = "signature";
    public const string IdField = "id";
    public const string AppIdField = "appId";
    public const string StatusField = "status";
    
    private static Dictionary<string, OrderStatusType> _orderStatusDict = new()
    {
        { "FINISHED", OrderStatusType.Finish },
        { "PAY_FAIL", OrderStatusType.Failed },
        { "PAY_SUCCESS", OrderStatusType.Pending },
        { "NEW", OrderStatusType.Created },
        { "TIMEOUT", OrderStatusType.Expired },
        
        { "1", OrderStatusType.Created },
        { "2", OrderStatusType.UserCompletesCoinDeposit },
        { "3", OrderStatusType.StartPayment },
        { "4", OrderStatusType.SuccessfulPayment },
        { "5", OrderStatusType.PaymentFailed },
        { "6", OrderStatusType.RefundSuccessfully },
        { "7", OrderStatusType.Expired },
    };
    
    public static bool OrderStatusExist(string orderStatus)
    {
        return GetOrderStatus(orderStatus) != OrderStatusType.Unknown;
    }

    public static OrderStatusType GetOrderStatus(string status)
    {
        var exists = _orderStatusDict.TryGetValue(status, out var statusEnum);
        return exists ? statusEnum : OrderStatusType.Unknown;
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

    public static string HmacSign(string source, string key)
    {
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(key));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(source)));
    }

    public static string GetOrderTransDirectForQuery(string orderDataTransDirect)
    {
        switch (orderDataTransDirect)
        {
            case string s when s.ToLower().Contains("sell"): return OrderTransDirect.SELL.ToString();
            case string s when s.ToLower().Contains("buy"): return OrderTransDirect.BUY.ToString();
            default: return OrderTransDirect.SELL.ToString();
        }
    }
    
    // Generate Alchemy request sigh by "appId + appSecret + timestamp".
    public static string GenerateAlchemyApiSign(string source)
    {
        var bytes = Encoding.UTF8.GetBytes(source);
        var hashBytes = SHA1.Create().ComputeHash(bytes);

        var sb = new StringBuilder();
        foreach (var t in hashBytes)
        {
            sb.Append(t.ToString("X2"));
        }
        return sb.ToString().ToLower();
    }
}