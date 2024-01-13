using System;
using System.Security.Cryptography;
using System.Text;
using SignatureServer.Common;
using SignatureServer.Dtos;

namespace SignatureServer.Providers.ExecuteStrategy;

public class AlchemyPayAesSignStrategy : IThirdPartExecuteStrategy<CommonThirdPartExecuteInput, CommonThirdPartExecuteOutput>
{
    
    public ThirdPartExecuteStrategy ExecuteStrategy()
    {
        return ThirdPartExecuteStrategy.AlchemyPayAes;
    }

    public CommonThirdPartExecuteOutput Execute(string secret, CommonThirdPartExecuteInput input)
    {
        var plainTextData = Encoding.UTF8.GetBytes(input.BizData);
        var secretKey = Encoding.UTF8.GetBytes(secret);
        var iv = Encoding.UTF8.GetString(secretKey).Substring(0, 16);
        var aesAlgorithm = new AesManaged();
        aesAlgorithm.Mode = CipherMode.CBC;
        aesAlgorithm.Padding = PaddingMode.PKCS7;
        var plaintextLength = plainTextData.Length;
        var plaintext = new byte[plaintextLength];
        Array.Copy(plainTextData, 0, plaintext, 0, plainTextData.Length);
        var encryptor = aesAlgorithm.CreateEncryptor(secretKey, Encoding.UTF8.GetBytes(iv));
        var encryptedData = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        var result = Convert.ToBase64String(encryptedData);
        return new CommonThirdPartExecuteOutput(result);
    }
    
}