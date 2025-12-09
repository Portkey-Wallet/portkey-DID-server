using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer.Device;

public interface IEncryptionProvider
{
    string AESEncrypt(string encryptStr, string key, string vector);
    string AESDecrypt(string decryptStr, string key, string vector);
}

public class EncryptionProvider : IEncryptionProvider, ISingletonDependency
{
    private readonly ILogger<EncryptionProvider> _logger;

    public EncryptionProvider(ILogger<EncryptionProvider> logger)
    {
        _logger = logger;
    }

    public string AESEncrypt(string encryptStr, string key, string vector)
    {
        try
        {
            byte[] encrypted;
            using (var aes = new AesManaged())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = Encoding.UTF8.GetBytes(vector);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                byte[] plainBytes = Encoding.UTF8.GetBytes(encryptStr);
                encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            }

            return Convert.ToBase64String(encrypted);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "AESEncrypt Error: {str}", encryptStr);
            throw new Exception("AESEncrypt Error");
        }
    }

    public string AESDecrypt(string decryptStr, string key, string vector)
    {
        try
        {
            var encrypted = Convert.FromBase64String(decryptStr);
            string decrypted;

            using (var aes = new AesManaged())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = Encoding.UTF8.GetBytes(vector);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                var plainBytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                decrypted = Encoding.UTF8.GetString(plainBytes);
            }

            return decrypted;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "AESDecrypt Error: {str}", decryptStr);
            throw new Exception("AESDecrypt Error");
        }
    }
}