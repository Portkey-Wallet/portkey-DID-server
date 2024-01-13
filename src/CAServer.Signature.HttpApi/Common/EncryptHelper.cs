using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AElf;

namespace SignatureServer.Common;

public static class EncryptHelper
{
    private const int KeySize = 256;
    private const int BlockSize = 128;
    private const int Iterations = 1000;


    public static byte[] AesGcmEncrypt(byte[] data, byte[] key, byte[] nonce, out byte[] tag)
    {
        using var aesGcm = new AesGcm(key);
        tag = new byte[16];
        var encryptedData = new byte[data.Length];
        aesGcm.Encrypt(nonce, data, encryptedData, tag);
        return encryptedData;
    }

    public static byte[] AesGcmDecrypt(byte[] encryptedData, byte[] key, byte[] nonce, byte[] tag)
    {
        using var aesGcm = new AesGcm(key);
        var decryptedData = new byte[encryptedData.Length];
        aesGcm.Decrypt(nonce, encryptedData, tag, decryptedData);
        return decryptedData;
    }
 
    
    public static string AesCbcEncrypt(string sourceData, string password)
    {
        return AesCbcEncrypt(Encoding.UTF8.GetBytes(sourceData), Encoding.UTF8.GetBytes(password)).ToHex();
    }
    
    public static string AesCbcDecrypt(string encryptData, string password)
    {
        return AesCbcDecrypt(Encoding.UTF8.GetBytes(encryptData), Encoding.UTF8.GetBytes(password)).ToHex();
    }
    
    public static byte[] AesCbcEncrypt(byte[] sourceData, byte[] password, byte[]? salt = null)
    {
        using var aesAlg = Aes.Create();
        aesAlg.KeySize = KeySize;
        aesAlg.BlockSize = BlockSize;
        aesAlg.Mode = CipherMode.CBC;
        aesAlg.Padding = PaddingMode.PKCS7;
        aesAlg.Key = new Rfc2898DeriveBytes(password, salt ?? new byte[8], Iterations).GetBytes(KeySize / 8);
        aesAlg.GenerateIV();
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
        aesAlg.Key = new Rfc2898DeriveBytes(password, salt ?? new byte[8], Iterations).GetBytes(KeySize / 8);
        var iv = new byte[aesAlg.BlockSize / 8];
        Array.Copy(encryptData, 0, iv, 0, iv.Length);
        aesAlg.IV = iv;
        var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, iv);
        using var msDecrypt = new MemoryStream();
        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
        {
            csDecrypt.Write(encryptData, iv.Length, encryptData.Length - iv.Length);
        }
        return msDecrypt.ToArray();
    }
}