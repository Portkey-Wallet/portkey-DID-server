using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using CAServer.Options;
using CAServer.ThirdPart.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace CAServer.ThirdPart.NftCheckout;

public partial class MerchantTest
{
    
    
    private static IOptions<ThirdPartOptions> MockThirdPartOptions()
    {
        var thirdPartOptions = new ThirdPartOptions()
        {
            Alchemy = new AlchemyOptions()
            {
                AppId = "test",
                AppSecret = "testTest",
                BaseUrl = "http://localhost:9200/book/_search",
            },
            Timer = new ThirdPartTimerOptions()
            {
                TimeoutMillis = 5000,
                DelaySeconds = 1,
            },
            Merchant = new MerchantOptions
            {
                DidPrivateKey = new Dictionary<string, string>
                {
                    ["symbolMarket"] = "5945c176c4269dc2aa7daf7078bc63b952832e880da66e5f2237cdf79bc59c5f"
                },
                MerchantPublicKey = new Dictionary<string, string>
                {
                    ["symbolMarket"] = "042dc50fd7d211f16bf4ad870f7790d4f9d98170f3712038c45830947f7d96c691ef2d1ab4880eeeeafb63ab77571be6cbe6bed89d5f89844b0fb095a7015713c8"
                }
            }
        };
        return new OptionsWrapper<ThirdPartOptions>(thirdPartOptions);
    }
    
}