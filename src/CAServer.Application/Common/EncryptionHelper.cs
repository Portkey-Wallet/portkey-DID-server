using System.Security.Cryptography;
using System.Text;

namespace CAServer.Common;

public static class EncryptionHelper
{
    /// <summary>
    /// 32位MD5加密
    /// </summary>
    /// <param name="password"></param>
    /// <returns></returns>
    public static string MD5Encrypt32(string input)
    {
        var result = "";
        byte[] computeHash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(input));
        for (var i = 0; i < computeHash.Length; i++)
        {
            result = result + computeHash[i].ToString("X");
        }

        return result;
    }
}