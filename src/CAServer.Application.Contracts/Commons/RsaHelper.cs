using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;

namespace CAServer.Commons;

public class RsaHelper
{
    public static AsymmetricCipherKeyPair GenerateRsaKeyPair(byte[] seed = null)
    {
        SecureRandom secure;
        if (seed == null)
        {
            secure = new SecureRandom();
        }
        else
        {
            secure = SecureRandom.GetInstance("SHA1PRNG", false);
            secure.SetSeed(seed);
        }
        
        var generator = new RsaKeyPairGenerator();
        generator.Init(new KeyGenerationParameters(secure, 2048));
        return generator.GenerateKeyPair();
    }

}