using System;
using System.Collections.Generic;
using CAServer.ThirdPart.Dtos;
using System.Security.Cryptography;
using System.Text;

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

public static class AlchemyHelper
{
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
        else
        {
            return "Unknown";
        }
    }

    public static string AESEncrypt(string plainText, string secretKeyData)
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
    public static string ComputeHmacsha256(string message, string secretKey)
    {
        var encoding = new ASCIIEncoding();
        byte[] keyByte = encoding.GetBytes(secretKey);
        byte[] messageBytes = encoding.GetBytes(message);
        using (var hmacsha256 = new HMACSHA256(keyByte))
        {
            byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
            return BitConverter.ToString(hashmessage).Replace("-", "").ToLower();
        }
    }
}