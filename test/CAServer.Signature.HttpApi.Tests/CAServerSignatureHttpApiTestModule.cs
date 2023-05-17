using System;
using System.Collections.Generic;
using System.Linq.Dynamic.Core.Tokenizer;
using System.Security.Cryptography;
using System.Text;
using AElf;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace CAServer.Signature.Test;

[DependsOn(
    typeof(CAServerSignatureHttpApiModule)
)]
// [DependsOn(
//     typeof(CAServerApplicationModule),
//     typeof(AbpEventBusModule),
//     typeof(CAServerGrainTestModule),
//     typeof(CAServerDomainTestModule)
// )]
public class CAServerSignatureHttpApiTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Configure<AbpAutoMapperOptions>(options => { options.AddMaps<CAServerSignatureHttpApiModule>(); });
        byte[] privateKeyBytes = Encoding.UTF8.GetBytes("test.1234567890.key");
        context.Services.Configure<SignatureOptions>(option =>
        {
            option.PrivateKeyDictionary = new Dictionary<string, string>
            {
                { "test-key", privateKeyBytes.ToHex() }
            };
        });
        base.ConfigureServices(context);
    }
}