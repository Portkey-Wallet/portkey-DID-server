using System.Collections.Generic;
using AElf;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using Microsoft.Extensions.DependencyInjection;
using SignatureServer;
using Volo.Abp.Modularity;

namespace CAServer.Signature.Test;

[DependsOn(
    typeof(CAServerSignatureHttpApiModule),
    typeof(CAServerSignatureHttpApiModule)
)]
public class CAServerSignatureHttpApiTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ECKeyPair keyPair = CryptoHelper.GenerateKeyPair();
        string hex1 = keyPair.PrivateKey.ToHex();
        string hex2 = keyPair.PublicKey.ToHex();
        byte[] privateKeyBytes = ByteArrayHelper.HexStringToByteArray(hex1);
        context.Services.Configure<KeyPairInfoOptions>(option =>
        {
            option.PrivateKeyDictionary = new Dictionary<string, string>
            {
                { "test-key", privateKeyBytes.ToHex() }
            };
        });
        base.ConfigureServices(context);
    }
}