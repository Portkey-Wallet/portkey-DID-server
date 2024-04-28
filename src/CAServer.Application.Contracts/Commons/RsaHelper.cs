using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

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

    public static byte[] ConvertPrivateKeyToDer(AsymmetricKeyParameter privateKey)
    {
        var privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(privateKey);
        return privateKeyInfo.GetDerEncoded();
    }

    public static byte[] ConvertPublicKeyToDer(AsymmetricKeyParameter publicKey)
    {
        var publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(publicKey);
        return publicKeyInfo.GetDerEncoded();
    }
}