using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CAServer.Common;

public static class EncryptionHelper
{
    private const int Iterations = 1000;


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
    
    public static string Encrypt(string plainText, string password)
    {
        var aes = Aes.Create();
        aes.KeySize = 256; 
        aes.BlockSize = 128; 
        aes.Mode = CipherMode.CBC; 

        var salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        var key = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);

        using (var encryptor = aes.CreateEncryptor(key.GetBytes(aes.KeySize / 8), aes.IV))
        using (var memoryStream = new MemoryStream())
        {
            memoryStream.Write(salt, 0, salt.Length); 
            memoryStream.Write(aes.IV, 0, aes.IV.Length);
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            {
                using (var streamWriter = new StreamWriter(cryptoStream))
                {
                    streamWriter.Write(plainText);
                }
            }

            return Convert.ToBase64String(memoryStream.ToArray());
        }
    }

    public static string Decrypt(string cipherText, string password)
    {
        var bytes = Convert.FromBase64String(cipherText);
        using (var memoryStream = new MemoryStream(bytes))
        {
            var salt = new byte[16];
            var _ = memoryStream.Read(salt, 0, salt.Length);

            var iv = new byte[16];
            var __= memoryStream.Read(iv, 0, iv.Length);

            var key = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);

            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(key.GetBytes(aes.KeySize / 8), aes.IV))
                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                using (var streamReader = new StreamReader(cryptoStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
    }

}