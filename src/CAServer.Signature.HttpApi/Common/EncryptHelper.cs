using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using AElf;

namespace SignatureServer.Common;

public static class EncryptHelper
{
  
    public static byte[] AesEncrypt(byte[] sourceData, byte[] password, byte[] salt)
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

    public static byte[] AesDecrypt(byte[] encryptData, byte[] password, byte[] salt)
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