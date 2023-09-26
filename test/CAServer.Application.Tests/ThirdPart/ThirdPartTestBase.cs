using System.Collections.Generic;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Options;
using Microsoft.Extensions.Options;
using Moq;

namespace CAServer.ThirdPart;

public class ThirdPartTestBase : CAServerApplicationTestBase
{

    protected static IOptionsSnapshot<GraphQLOptions> MockGraphQLOptions()
    {
        var mockOptions = new Mock<IOptionsSnapshot<GraphQLOptions>>();
        var option = new GraphQLOptions
        {
            GraphQLConnection = "http://localhost:9200/_search"
        };
        mockOptions.Setup(m => m.Value).Returns(option);
        return mockOptions.Object;
    }
    
    protected static IOptions<ThirdPartOptions> MockThirdPartOptions()
    {
        var thirdPartOptions = new ThirdPartOptions()
        {
            Alchemy = new AlchemyOptions()
            {
                AppId = "ramp",
                AppSecret = "rampTest",
                BaseUrl = "http://localhost:9200/book/_search",
                NftAppId = "test",
                NftAppSecret = "testTest",
                NftBaseUrl = "http://localhost:9200/book/_search",
                UpdateSellOrderUri = "/webhooks/off/merchant",
                FiatListUri = "/merchant/fiat/list",
                CryptoListUri = "/merchant/crypto/list",
                OrderQuoteUri = "/merchant/order/quote",
                GetTokenUri = "/merchant/getToken",
                MerchantQueryTradeUri = "/merchant/query/trade"
            },
            Timer = new ThirdPartTimerOptions()
            {
                TimeoutMillis = 5000,
                DelaySeconds = 1,
                HandleUnCompletedOrderMinuteAgo = 0,
                NftCheckoutResultThirdPartNotifyCount = 0,
                NftUnCompletedMerchantCallbackMinuteAgo = 0
            },
            Merchant = new MerchantOptions
            {
                DidPrivateKey = new Dictionary<string, string>
                {
                    ["symbolMarket"] = "5945c176c4269dc2aa7daf7078bc63b952832e880da66e5f2237cdf79bc59c5f"
                },
                MerchantPublicKey = new Dictionary<string, string>
                {
                    ["symbolMarket"] =
                        "042dc50fd7d211f16bf4ad870f7790d4f9d98170f3712038c45830947f7d96c691ef2d1ab4880eeeeafb63ab77571be6cbe6bed89d5f89844b0fb095a7015713c8"
                }
            }
        };
        return new OptionsWrapper<ThirdPartOptions>(thirdPartOptions);
    }

}