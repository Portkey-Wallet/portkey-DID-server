using System;
using System.Security.Cryptography;
using System.Text;

namespace CAServer.Common;

public static class GuidHelper
{
    public static Guid UniqId(params string[] paramArr)
    {
        return new Guid(MD5.HashData(Encoding.Default.GetBytes(string.Join("_", paramArr))));
    }
    
}