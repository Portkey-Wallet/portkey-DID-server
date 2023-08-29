using System.Collections.Generic;
using AElf.Types;
using CAServer.Common;
using CAServer.Security.Dtos;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using CAServer.UserSecurityAppService.Provider;
using Moq;
using Portkey.Contracts.CA;
using TokenInfo = CAServer.UserAssets.Provider.TokenInfo;

namespace CAServer.Security;

public partial class UserSecurityAppServiceTest
{
    private IUserAssetsProvider GetMockUserAssetsProvider()
    {
        var mockUserAssetsProvider = new Mock<IUserAssetsProvider>();
        mockUserAssetsProvider.Setup(m =>
            m.SearchUserAssetsAsync(It.IsAny<List<CAAddressInfo>>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<int>())).ReturnsAsync(
            new IndexerSearchTokenNfts
            {
                CaHolderSearchTokenNFT = new CaHolderSearchTokenNFT
                {
                    TotalRecordCount = 1,
                    Data = new List<IndexerSearchTokenNft>
                    {
                        new()
                        {
                            ChainId = "AELF",
                            CaAddress = "c1pPpwKdVaYjEsS5VLMTkiXf76wxW9YY2qaDBPowpa8zX2oEo",
                            Balance = 1000,
                            TokenId = 1,
                            TokenInfo = new TokenInfo
                            {
                                Symbol = "ELF",
                                Decimals = 8,
                                TokenContractAddress = "address",
                                TokenName = "tokenName",
                                TotalSupply = 1000
                            },
                            NftInfo = new NftInfo
                            {
                                Symbol = "ELF-NFT"
                            }
                        }
                    }
                }
            });
        return mockUserAssetsProvider.Object;
    }

    private IUserSecurityProvider GetMockUserSecurityProvider()
    {
        var mockUserSecurityProvider = new Mock<IUserSecurityProvider>();

        mockUserSecurityProvider.Setup(m => m.GetTransferLimitListByCaHash(It.IsAny<string>()))
            .ReturnsAsync(
                new IndexerTransferLimitList()
                {
                    CaHolderTransferLimit = new CaHolderTransferLimit()
                    {
                        TotalRecordCount = 1,
                        Data = new List<TransferLimitDto>()
                        {
                            new TransferLimitDto()
                            {
                                ChainId = "AELF",
                                Symbol = "ELF",
                                DailyLimit = 10000,
                                SingleLimit = 10000
                            }
                        }
                    }
                });

        return mockUserSecurityProvider.Object;
    }

    private IContractProvider GetContractProvider()
    {
        var fromBase58 = Address.FromBase58("NJRa6TYqvAgfDsLnsKXCA2jt3bYLEA8rUgPPzwMAG3YYXviHY");

        var mockContractProvider = new Mock<IContractProvider>();
        mockContractProvider.Setup(m =>
                m.GetHolderInfoAsync(Hash.LoadFromHex("a8ae393ecb7cba148d269c262993eacb6a1b25b4dc55270b55a9be7fc2412033"), null, It.IsAny<string>()))
            .ReturnsAsync(new GetHolderInfoOutput
                {
                    CaAddress = fromBase58,
                    CaHash = Hash.LoadFromHex("a8ae393ecb7cba148d269c262993eacb6a1b25b4dc55270b55a9be7fc2412033")

                }
            );

        return mockContractProvider.Object;
    }
}