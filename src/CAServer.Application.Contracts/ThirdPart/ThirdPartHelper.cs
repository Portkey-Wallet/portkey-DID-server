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

    public static string GetOrderTransDirectForQuery(string orderDataTransDirect)
    {
        switch (orderDataTransDirect)
        {
            case string s when s.ToLower().Contains("sell"): return OrderTransDirect.SELL.ToString();
            case string s when s.ToLower().Contains("buy"): return OrderTransDirect.BUY.ToString();
            default: return OrderTransDirect.SELL.ToString();
        }
    }
}