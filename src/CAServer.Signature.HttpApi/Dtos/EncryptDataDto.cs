using System;
using System.Text;
using AElf;
using Newtonsoft.Json;
using SignatureServer.Command;
using SignatureServer.Common;
using Volo.Abp;
using Random = System.Random;

namespace SignatureServer.Dtos;

public class EncryptDataDto
{

    public int Version { get; set; } = 1;
    public string EncryptType { get; set; }
    public string Salt { get; set; }
    public string Key { get; set; }
    public string Information { get; set; }
    public string EncryptData { get; set; }


    public string Decrypt(string password)
    {
        try
        {
            var cipherText = ByteArrayHelper.HexStringToByteArray(EncryptData);
            var key = HashHelper.ComputeFrom(password).ToByteArray();
            var salt = ByteArrayHelper.HexStringToByteArray(Salt);
            return Encoding.UTF8.GetString(EncryptHelper.AesDecrypt(cipherText, key, salt));
        }
        catch (Exception e)
        {
            throw;
            // throw new Exception("Decrypt data failed", e);
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
        public const EncryptType DefaultEncryptType = Command.EncryptType.AES_CBC;
        
        public bool PasswordMatch { get; set; }
        
        private string? _password;
        private string _secret = "";
        private readonly EncryptDataDto _instance = new ();

        public EncryptDataDto Build()
        {
            if (_password.IsNullOrEmpty()) throw new ArgumentException("password");
            if (_secret.IsNullOrEmpty()) throw new ArgumentException("secret");
            if (_instance.Salt.IsNullOrEmpty()) throw new ArgumentException("salt");

            var passwordByte = ByteArrayHelper.HexStringToByteArray(_password);
            var saltByte = ByteArrayHelper.HexStringToByteArray(_instance.Salt);
            
            _instance.EncryptType = _instance.EncryptType.IsNullOrEmpty()
                ? DefaultEncryptType.ToString()
                : _instance.EncryptType;
            
            _instance.EncryptData = _instance.EncryptType == DefaultEncryptType.ToString() 
                ? EncryptHelper.AesEncrypt(Encoding.UTF8.GetBytes(_secret), passwordByte, saltByte).ToHex()
                : "Invalid encrypt type";
            
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
        
        public EncryptDataBuilder Information(string information)
        {
            _instance.Information = information;
            return this;
        }
        
        public EncryptDataBuilder Password(string password)
        {
            _password = HashHelper.ComputeFrom(password).ToHex();
            PasswordMatch = false;
            return this;
        }
        
        public EncryptDataBuilder RepeatPassword(string repeatPassword)
        {
            PasswordMatch = !_password.IsNullOrEmpty() && _password == HashHelper.ComputeFrom(repeatPassword).ToHex();
            return this;
        }
        
        public EncryptDataBuilder RandomSalt()
        {
            _instance.Salt = HashHelper.ComputeFrom(Random.Shared.NextInt64()).ToHex();
            return this;
        }
        
        
    }

}