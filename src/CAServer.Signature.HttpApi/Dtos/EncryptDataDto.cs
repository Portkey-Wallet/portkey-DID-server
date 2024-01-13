using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using AElf;
using Newtonsoft.Json;
using SignatureServer.Common;
using Volo.Abp;

namespace SignatureServer.Dtos;

public class EncryptDataDto
{
    
    public int Version { get; set; } = 1;
    public string EncryptType { get; set; }
    public string Nonce { get; set; }
    public string Tag { get; set; }
    public string Key { get; set; }
    public string Information { get; set; }
    public string EncryptData { get; set; }


    public string Decrypt(string password)
    {
        var cipherText = ByteArrayHelper.HexStringToByteArray(EncryptData);
        var key = HashHelper.ComputeFrom(password).ToByteArray();
        var nonce = ByteArrayHelper.HexStringToByteArray(Nonce);
        var tag = Tag.IsNullOrEmpty() ? Array.Empty<byte>() : ByteArrayHelper.HexStringToByteArray(Tag);
        return EncryptType switch
            {
                Command.EncryptType.AesCbc => Encoding.UTF8.GetString(EncryptHelper.AesCbcDecrypt(cipherText, key, nonce)),
                Command.EncryptType.AesGcm => Encoding.UTF8.GetString(EncryptHelper.AesGcmDecrypt(cipherText, key, nonce, tag)),
                _ => throw new UserFriendlyException("Invalid encrypt type")
            };
    }

    public string Encrypt(string password, string secret)
    {
        var passwordByte = ByteArrayHelper.HexStringToByteArray(password);
        var nonceByte = ByteArrayHelper.HexStringToByteArray(Nonce);
        var tag = Array.Empty<byte>();
        EncryptData = EncryptType switch
        {
            Command.EncryptType.AesCbc => EncryptHelper
                .AesCbcEncrypt(Encoding.UTF8.GetBytes(secret), passwordByte, nonceByte).ToHex(),
            Command.EncryptType.AesGcm => EncryptHelper
                .AesGcmEncrypt(Encoding.UTF8.GetBytes(secret), passwordByte, nonceByte, out tag).ToHex(),
            _ => throw new UserFriendlyException("Invalid encrypt type")
        };
        
        // for AES_GCM
        if (!tag.IsNullOrEmpty()) Tag = tag.ToHex();
        return EncryptData;
    }


    public bool TryDecrypt(string password, out string secretData, out string message)
    {
        try
        {
            secretData = Decrypt(password);
            message = "success";
            return true;
        }
        catch (Exception e)
        {
            message = e.Message;
            secretData = "";
            return false;
        }
    }

    public string FormatJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    public static EncryptDataBuilder Builder()
    {
        return new EncryptDataBuilder();
    }

    public class EncryptDataBuilder
    {
        private string? _password;
        private string? _repeatPassword;
        private string _secret = "";
        private readonly EncryptDataDto _instance = new();

        public EncryptDataDto Build()
        {
            if (_password.IsNullOrEmpty()) throw new ArgumentException("password");
            if (_secret.IsNullOrEmpty()) throw new ArgumentException("secret");
            if (_instance.Nonce.IsNullOrEmpty()) throw new ArgumentException("salt");
            if (_password != _repeatPassword) throw new ArgumentException("password not match");
            _instance.Encrypt(_password!, _secret);
            return _instance;
        }

        public EncryptDataBuilder Secret(string secret)
        {
            _secret = secret;
            return this;
        }

        public EncryptDataBuilder Key(string key)
        {
            _instance.Key = key;
            return this;
        }

        public EncryptDataBuilder EncryptType(string type, out bool success)
        {
            success = false;
            if (type.IsNullOrEmpty())
            {
                _instance.EncryptType = Command.EncryptType.Default;
                Console.WriteLine($"( Default encrypt type {Command.EncryptType.Default} will be used)");
                success = true;
            }
            else if (Command.EncryptType.All.Contains(type))
            {
                success = true;
                _instance.EncryptType = type;
            }

            return this;
        }

        public EncryptDataBuilder Information(string information)
        {
            _instance.Information = information;
            return this;
        }

        public EncryptDataBuilder Password(string password)
        {
            _password = HashHelper.ComputeFrom(password).ToHex();
            _repeatPassword = null;
            return this;
        }

        public EncryptDataBuilder RepeatPassword(string repeatPassword, out bool success)
        {
            _repeatPassword = HashHelper.ComputeFrom(repeatPassword).ToHex();
            success = !_password.IsNullOrEmpty() && _password == _repeatPassword;
            return this;
        }

        public EncryptDataBuilder RandomNonce()
        {
            var nonce = new byte[12];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonce);
            }
            _instance.Nonce = nonce.ToHex();
            return this;
        }
    }
}