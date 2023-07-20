using System;

namespace CAServer.Common;

public class HashObj
{
    public byte[] Value { get; set; }


    public HashObj(string hex)
    {
        Value = HexToBytes(hex);
    }

    public string ToHex()
    {
        return BitConverter.ToString(Value).Replace("-", string.Empty);
    }

    public static byte[] HexToBytes(string hex)
    {
        var result = new byte[hex.Length / 2];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return result;
    }

    
}