using System.Collections.Generic;
using AElf;
using AElf.Types;
using CAServer.Common;
using CAServer.Security.Dtos;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using CAServer.UserSecurity.Provider;
using Moq;
using Nethereum.Hex.HexConvertors.Extensions;
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

        mockUserSecurityProvider.Setup(m => m.GetTransferLimitListByCaHashAsync(It.IsAny<string>()))
            .ReturnsAsync(
                new IndexerTransferLimitList()
                {
                    CaHolderTransferLimit = new CaHolderTransferLimit()
                    {
                        TotalRecordCount = 1,
                        Data = new List<TransferLimitDto>()
                        {
                            new TransferLimitDto
                            {
                                ChainId = "AELF",
                                Symbol = "ELF",
                                DailyLimit = "10000",
                                SingleLimit = "10000"
                            }
                        }
                    }
                });
        mockUserSecurityProvider.Setup(m => m.GetManagerApprovedListByCaHashAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>()))
            .ReturnsAsync(
                new IndexerManagerApprovedList()
                {
                    CaHolderManagerApproved = new CaHolderManagerApproved()
                    {
                        TotalRecordCount = 1,
                        Data = new List<ManagerApprovedDto>()
                        {
                            new ManagerApprovedDto
                            {
                                ChainId = "AELF",
                                CaHash = HashHelper.ComputeFrom("test@google.com").ToHex(),
                                Spender = Address.FromPublicKey("AAA".HexToByteArray()).ToBase58(),
                                Symbol = "ELF",
                                Amount = 10000,
                            }
                        }
                    }
                });
        return mockUserSecurityProvider.Object;
    }

    private IContractProvider GetContractProvider()
    {
        var caAddress = Address.FromPublicKey("AAA".HexToByteArray());
        var caHash = HashHelper.ComputeFrom("test@google.com");

        var mockContractProvider = new Mock<IContractProvider>();
        mockContractProvider.Setup(m => m.GetHolderInfoAsync(caHash, null, It.IsAny<string>()))
            .ReturnsAsync(new GetHolderInfoOutput
                {
                    CaAddress = caAddress,
                    CaHash = caHash
                }
            );

        return mockContractProvider.Object;
    }
}