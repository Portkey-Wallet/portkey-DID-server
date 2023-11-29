using System;
using System.Collections.Generic;
using System.Linq.Dynamic.Core.Tokenizer;
using System.Security.Cryptography;
using System.Text;
using AElf;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
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
        context.Services.Configure<KeyStoreOptions>(o =>
        {
            o.KeyStorePath =
                "/Users/jasonlu/.local/share/aelf/keys/2iLUeZGW4xdgyUNUUiMDDejuSvq9Mc5gZsFxKSutp6Cr78ZrDx.json";
            o.KeyStorePassword = "admin123";
        });
        base.ConfigureServices(context);
    }
}