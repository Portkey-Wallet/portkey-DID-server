using System.Collections.Generic;
using CAServer.UserAssets;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using Moq;

namespace CAServer.CAAccount;

public partial class RecoveryServiceTests
{
    private IUserAssetsProvider GetMockUserAssetsProvider()
    {
        var mockUserAssetsProvider = new Mock<IUserAssetsProvider>();

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
}