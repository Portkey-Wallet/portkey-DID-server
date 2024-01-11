using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using AElf;

namespace SignatureServer.Common;

public static class EncryptHelper
{
  
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
    
    public static byte[] AesCbcEncrypt(byte[] sourceData, byte[] password, byte[] salt)
    {
        if (sourceData.IsNullOrEmpty())
        {
            return sourceData;
        }

        using var key = new Rfc2898DeriveBytes(password.ToHex(), salt);
        using var aesAlg = new RijndaelManaged();
        aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
        aesAlg.Mode = CipherMode.CBC;
        aesAlg.Padding = PaddingMode.PKCS7;

        var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
        using var msEncrypt = new MemoryStream();
        msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);

        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        {
            csEncrypt.Write(sourceData, 0, sourceData.Length);
        }

        return msEncrypt.ToArray();
    }

    public static byte[] AesCbcDecrypt(byte[] encryptData, byte[] password, byte[] salt)
    {
        if (encryptData.IsNullOrEmpty())
        {
            return encryptData;
        }

        using var key = new Rfc2898DeriveBytes(password.ToHex(), salt);
        using var aesAlg = new RijndaelManaged();
        aesAlg.Mode = CipherMode.CBC;
        aesAlg.Padding = PaddingMode.PKCS7;

        var iv = new byte[aesAlg.BlockSize / 8];
        Array.Copy(encryptData, 0, iv, 0, iv.Length);
        aesAlg.IV = iv;

        var decryptor = aesAlg.CreateDecryptor(key.GetBytes(aesAlg.KeySize / 8), iv);
        using var msDecrypt = new MemoryStream();
        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
        {
            csDecrypt.Write(encryptData, iv.Length, encryptData.Length - iv.Length);
        }
        return msDecrypt.ToArray();
    }
    
}