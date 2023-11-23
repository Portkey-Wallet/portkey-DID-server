using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using AElf;
using AElf.Client.Dto;
using AElf.Types;
using CAServer.Commons;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.Options;
using CAServer.ThirdPart.Dtos.ThirdPart;
using CAServer.ThirdPart.Transak;
using CAServer.Tokens.Provider;
using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit.Abstractions;

namespace CAServer.ThirdPart;

public class ThirdPartTestBase : CAServerApplicationTestBase
{

    internal readonly string PendingTxId = HashHelper.ComputeFrom("PENDING").ToHex();
    internal readonly string MinedTxId = HashHelper.ComputeFrom("MINED").ToHex();


    public ThirdPartTestBase(ITestOutputHelper output) : base(output)
    {
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockHttpFactory());
        MockHttpByPath(TransakApi.RefreshAccessToken.Method, TransakApi.RefreshAccessToken.Path, new TransakMetaResponse<object, TransakAccessToken>
        {
            Data = new TransakAccessToken
            {
                AccessToken = "TransakAccessTokenMockData",
                ExpiresAt = DateTime.UtcNow.AddHours(2).ToUtcSeconds()
            }
        });
        MockHttpByPath(TransakApi.UpdateWebhook.Method, TransakApi.UpdateWebhook.Path, "success");
    }


    protected static IOptionsMonitor<ThirdPartOptions> MockThirdPartOptions()
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
            Transak = new TransakOptions
            {
                AppId = "transakAppId",
                AppSecret = "transakAppSecret",
                BaseUrl = "http://127.0.0.1:9200"
            },
            OrderExportAuth = new OrderExportAuth
            {
                Key = "test"
            },
            Timer = new ThirdPartTimerOptions()
            {
                TimeoutMillis = 5000,
                DelaySeconds = 1,
                HandleUnCompletedOrderMinuteAgo = 0,
                NftUnCompletedMerchantCallbackMinuteAgo = 0,
                HandleUnCompletedSettlementTransferSecondsAgo = 0,
            },
            Merchant = new MerchantOptions
            {
                Merchants = new Dictionary<string, MerchantItem>
                {
                    ["symbolMarket"] = new MerchantItem
                    {
                        PublicKey = "042dc50fd7d211f16bf4ad870f7790d4f9d98170f3712038c45830947f7d96c691ef2d1ab4880eeeeafb63ab77571be6cbe6bed89d5f89844b0fb095a7015713c8",
                        DidPrivateKey = "5945c176c4269dc2aa7daf7078bc63b952832e880da66e5f2237cdf79bc59c5f"
                    }
                }
            }
        };
        
        var optionMock = new Mock<IOptionsMonitor<ThirdPartOptions>>();
        optionMock.Setup(o => o.CurrentValue).Returns(thirdPartOptions);
        return optionMock.Object;
    }

    protected static IOptionsMonitor<RampOptions> MockRampOptions()
    {
        var rampOption = new RampOptions
        {
            Providers = new Dictionary<string, ThirdPartProvider>
            {
                ["Alchemy"] = new()
                {
                    AppId = "test",
                    BaseUrl = "http://127.0.0.1:9200",
                    Name = "AlchemyPay",
                    Logo = "http://127.0.0.1:9200/logo.png",
                    WebhookUrl = "http://127.0.0.1:9200",
                    CountryIconUrl = "https://static.alchemypay.org/alchemypay/flag/{ISO}.png",
                    PaymentTags = new List<string>{"ApplePay", "GooglePay"},
                    Coverage = new ProviderCoverage
                    {
                        OffRamp = true,
                        OnRamp = true
                    }
                },
                ["Transak"] = new()
                {
                    AppId = "test",
                    BaseUrl = "http://127.0.0.1:9200",
                    Name = "TransakPay",
                    Logo = "http://127.0.0.1:9200/logo.png",
                    WebhookUrl = "http://127.0.0.1:9200",
                    CountryIconUrl = "https://static.alchemypay.org/alchemypay/flag/{ISO}.png",
                    PaymentTags = new List<string>{"ApplePay", "GooglePay"},
                    Coverage = new ProviderCoverage
                    {
                        OffRamp = true,
                        OnRamp = true
                    }
                }
            },
            PortkeyIdWhiteList = new List<string>(),
            DefaultCurrency = new DefaultCurrencyOption(),
            CryptoList = new List<CryptoItem>{ new()
            {
                Symbol = "ELF",
                Icon = "http://127.0.0.1:9200/elf.png",
                Decimals = "8",
                Network = "AELF-AELF",
                Address = "0x00000000"
            }},
            CoverageExpressions = new Dictionary<string, CoverageExpression>
            {
                ["Alchemy"] = new()
                {
                    OnRamp = new List<string>
                    {
                        "(baseCoverage || InList(portkeyId, portkeyIdWhitelist))",
                        "&& InList(clientType, List(\"WebSDK\",\"Chrome\"))"
                    },
                    OffRamp = new List<string>
                    {
                        "(baseCoverage || InList(portkeyId, portkeyIdWhitelist))",
                        "&& InList(clientType, List(\"WebSDK\",\"Chrome\"))"
                    }
                },
                ["Transak"] = new()
                {
                    OnRamp = new List<string>
                    {
                        "(baseCoverage || InList(portkeyId, portkeyIdWhitelist))",
                        "&& InList(clientType, List(\"WebSDK\",\"Chrome\"))"
                    },
                    OffRamp = new List<string>
                    {
                        "(baseCoverage || InList(portkeyId, portkeyIdWhitelist))",
                        "&& InList(clientType, List(\"WebSDK\",\"Chrome\"))"
                    }
                }
            }
        };
        
        
        var optionMock = new Mock<IOptionsMonitor<RampOptions>>();
        optionMock.Setup(o => o.CurrentValue).Returns(rampOption);
        return optionMock.Object;
    }

    protected MassTransit.IBus MockMassTransitIBus()
    {
        var mockContractProvider = new Mock<MassTransit.IBus>();
        return mockContractProvider.Object;
    }

    protected CAServer.Common.IContractProvider MockContractProvider()
    {
        var mockContractProvider = new Mock<CAServer.Common.IContractProvider>();
        mockContractProvider
            .Setup(p =>
                p.SendRawTransactionAsync("AELF", It.IsAny<string>()))
            .ReturnsAsync(new SendTransactionOutput{ TransactionId = PendingTxId });

        mockContractProvider
            .Setup(p => p.GetTransactionResultAsync(It.IsAny<string>(), PendingTxId))
            .ReturnsAsync(new TransactionResultDto
            {
                TransactionId = PendingTxId,
                Status = "PENDING",
                Transaction = new TransactionDto()
            });
        
        mockContractProvider
            .Setup(p => p.GetTransactionResultAsync(It.IsAny<string>(), MinedTxId))
            .ReturnsAsync(new TransactionResultDto
            {
                TransactionId = MinedTxId,
                Status = "MINED",
                Transaction = new TransactionDto()
            });
        
        mockContractProvider
            .Setup(p => p.GenerateTransferTransaction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Tuple<string, Transaction>(PendingTxId, new Transaction()));

        mockContractProvider
            .Setup(p => p.GetChainStatus(It.IsAny<string>()))
            .ReturnsAsync(new ChainStatusDto
            {
                BestChainHeight = 100,
                LongestChainHeight = 100,
                LastIrreversibleBlockHeight = 100,
                GenesisBlockHash = HashHelper.ComputeFrom("").ToHex()
            });
        return mockContractProvider.Object;
    }

    protected IGraphQLProvider MockGraphQlProvider()
    {
        var mockGraphQlClient = new Mock<IGraphQLProvider>();
        mockGraphQlClient
            .Setup(p => p.GetIndexBlockHeightAsync("AELF"))
            .ReturnsAsync(100);

        return mockGraphQlClient.Object;
    }

    protected ITokenProvider MockTokenPrivider()
    {
        
        var tokenProvider = new Mock<ITokenProvider>();
        tokenProvider
            .Setup(p => p.GetTokenInfosAsync(It.IsAny<string>(), "ELF", It.IsAny<string >(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new IndexerTokens
            {
                TokenInfo = new List<IndexerToken>
                {
                    new ()
                    {
                        Symbol = "ELF",
                        Decimals = 8,
                    }
                }
            });

        return tokenProvider.Object;
    }

}