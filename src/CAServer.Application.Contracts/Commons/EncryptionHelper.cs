using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AElf;

namespace CAServer.Commons;

public static class EncryptionHelper
{
    private const int KeySize = 256;
    private const int BlockSize = 128;
    private const int Iterations = 1000;
    private const int IvIterations = 1;


    public static string MD5Encrypt32(string input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }

            return sb.ToString();
        }
    }
    
    public static string EncryptBase64(string plainText, string password)
    {
        var encryptData = AesCbcEncrypt(Encoding.UTF8.GetBytes(plainText), Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(encryptData);
    }

    public static string DecryptFromBase64(string cipherText, string password)
    {
        var encryptData = Convert.FromBase64String(cipherText);
        return Encoding.UTF8.GetString(AesCbcDecrypt(encryptData, Encoding.UTF8.GetBytes(password)));
    }
    
    
    public static string EncryptHex(string plainText, string password)
    {
        var encryptData = AesCbcEncrypt(Encoding.UTF8.GetBytes(plainText), Encoding.UTF8.GetBytes(password));
        return encryptData.ToHex();
    }

    public static string DecryptFromHex(string cipherText, string password)
    {
        var encryptData = ByteArrayHelper.HexStringToByteArray(cipherText);
        return Encoding.UTF8.GetString(AesCbcDecrypt(encryptData, Encoding.UTF8.GetBytes(password)));
    }
    
    public static byte[] AesCbcEncrypt(byte[] sourceData, byte[] password, byte[]? salt = null)
    {
        using var aesAlg = Aes.Create();
        aesAlg.KeySize = KeySize;
        aesAlg.BlockSize = BlockSize;
        aesAlg.Mode = CipherMode.CBC;
        aesAlg.Padding = PaddingMode.PKCS7;
        aesAlg.Key = new Rfc2898DeriveBytes(password, salt ?? new byte[16], Iterations).GetBytes(KeySize / 8);
        if (salt.IsNullOrEmpty()) 
            aesAlg.IV = new Rfc2898DeriveBytes(password, new byte[16], IvIterations).GetBytes(BlockSize / 8);
        var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
        using var msEncrypt = new MemoryStream();
        msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        {
            csEncrypt.Write(sourceData, 0, sourceData.Length);
        }
        return msEncrypt.ToArray();
    }

    public static byte[] AesCbcDecrypt(byte[] encryptData, byte[] password, byte[]? salt = null)
    {
        using var aesAlg = Aes.Create();
        aesAlg.KeySize = KeySize;
        aesAlg.BlockSize = BlockSize;
        aesAlg.Mode = CipherMode.CBC;
        aesAlg.Padding = PaddingMode.PKCS7;
        aesAlg.Key = new Rfc2898DeriveBytes(password, salt ?? new byte[16], Iterations).GetBytes(KeySize / 8);
        var iv = new byte[aesAlg.BlockSize / 8];
        Array.Copy(encryptData, 0, iv, 0, iv.Length);
        var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, iv);
        using var msDecrypt = new MemoryStream();
        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
        {
            csDecrypt.Write(encryptData, iv.Length, encryptData.Length - iv.Length);
        }
        return msDecrypt.ToArray();
    }

}