using System;
using System.Collections.Generic;
using CAServer.Entities.Es;
using CAServer.Options;
using CAServer.UserAssets;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Options;
using Moq;
using TokenInfo = CAServer.UserAssets.Provider.TokenInfo;

namespace CAServer.CAAccount;

public partial class RecoveryServiceTests
{
    private IUserAssetsProvider GetMockUserAssetsProvider()
    {
        var mockUserAssetsProvider = new Mock<IUserAssetsProvider>();
        var uid = Guid.NewGuid();
        mockUserAssetsProvider.Setup(m => m.GetCaHolderIndexAsync(It.IsAny<Guid>())).ReturnsAsync(
            new CAHolderIndex
            {
                UserId = uid,
                CaHash = "",
                NickName = "MockName",
                IsDeleted = false,
                CreateTime = DateTime.Now
            });

        mockUserAssetsProvider.Setup(m => m.GetUserChainIdsAsync(It.IsAny<List<string>>())).ReturnsAsync(
            new IndexerChainIds
            {
                CaHolderManagerInfo = new List<UserChainInfo>
                {
                    new()
                    {
                        ChainId = "AELF"
                    },
                    new()
                    {
                        ChainId = "tDVV"
                    }
                }
            });

        mockUserAssetsProvider.Setup(m =>
                m.GetUserTokenInfoAsync(It.IsAny<List<CAAddressInfo>>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<int>()))
            .ReturnsAsync(new IndexerTokenInfos
            {
                CaHolderTokenBalanceInfo = new CaHolderTokenBalanceInfo
                {
                    totalRecordCount = 2,
                    Data = new List<IndexerTokenInfo>
                    {
                        new()
                        {
                            CaAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                            ChainId = "AELF",
                            Balance = 1000,
                            TokenIds = new List<long> { 1 },
                            TokenInfo = new TokenInfo
                            {
                                Symbol = "ELF",
                                Decimals = 8,
                                TokenContractAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                                TokenName = "Token",
                                TotalSupply = 10000
                            }
                        },
                        new()
                        {
                            CaAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                            ChainId = "AELF",
                            Balance = 1000,
                            TokenIds = new List<long> { 1 },
                            TokenInfo = new TokenInfo
                            {
                                Symbol = "CPU",
                                Decimals = 8,
                                TokenContractAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                                TokenName = "Token",
                                TotalSupply = 10000
                            }
                        }
                    }
                }
            });


        return mockUserAssetsProvider.Object;
    }
    
    
    private IOptions<ChainOptions> GetMockChainOptions()
    {
        var dict = new Dictionary<string, Options.ChainInfo>
        {
            ["MockChain"] = new()
            {
                ChainId = "MockChainId",
                BaseUrl = "http://localhost:8000",
                ContractAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                PrivateKey = "0x",
                TokenContractAddress = ""
            }
        };

        return new OptionsWrapper<ChainOptions>(new ChainOptions()
        {
            ChainInfos = dict
        });
    }
    
    
}