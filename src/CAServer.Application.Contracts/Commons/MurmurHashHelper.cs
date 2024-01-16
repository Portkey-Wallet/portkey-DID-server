using System.Text;

namespace CAServer.Commons;

public static class MurmurHashHelper
{
    public static string GenerateHash(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var hashConfig = new System.Data.HashFunction.MurmurHash.MurmurHash3Config
        {
            Seed = 0,
            HashSizeInBits = 32
        };

        var murmurHash = System.Data.HashFunction.MurmurHash.MurmurHash3Factory.Instance.Create(hashConfig);
        return murmurHash.ComputeHash(bytes).AsBase64String().TrimEnd('=').TrimEnd('=');
    }
}