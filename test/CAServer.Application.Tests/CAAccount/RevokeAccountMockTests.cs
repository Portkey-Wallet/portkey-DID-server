using System;
using System.Collections.Generic;
using AElf;
using AElf.Types;
using CAServer.AppleAuth.Provider;
using CAServer.CAAccount.Provider;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Guardian;
using CAServer.Guardian.Provider;
using CAServer.Options;
using CAServer.UserAssets;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Options;
using Moq;
using Nethereum.Hex.HexConvertors.Extensions;
using Portkey.Contracts.CA;
using GuardianDto = CAServer.Guardian.Provider.GuardianDto;

namespace CAServer.CAAccount;

public partial class RevokeAccountTests
{
    
    private IUserAssetsProvider GetMockUserAssetsProvider()
    {
        var mockUserAssetsProvider = new Mock<IUserAssetsProvider>();
        
        mockUserAssetsProvider.Setup(m =>
                m.GetUserTokenInfoAsync(It.IsAny<List<CAAddressInfo>>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<int>()))
            .ReturnsAsync(new IndexerTokenInfos
            {
                CaHolderTokenBalanceInfo = new CaHolderTokenBalanceInfo
                {
                    totalRecordCount = 0,
                    Data = new List<IndexerTokenInfo>()
                }
            });
        
        mockUserAssetsProvider.Setup(m =>
                m.GetCaHolderIndexByCahashAsync(It.IsAny<string>()))
            .ReturnsAsync(new CAHolderIndex()
            {
                CaHash = "a8ae393ecb7cba148d269c262993eacb6a1b25b4dc55270b55a9be7fc2412033"
            });
        
        
        mockUserAssetsProvider.Setup(m =>
                m.GetUserNftInfoAsync(It.IsAny<List<CAAddressInfo>>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<int>()))
            .ReturnsAsync(
                new IndexerNftInfos
                {
                    CaHolderNFTBalanceInfo = new CaHolderNFTBalanceInfo
                    {
                        TotalRecordCount = 0,
                        Data = new List<IndexerNftInfo>()
                    }
                });
        
        mockUserAssetsProvider.Setup(m => m.GetCaHolderIndexAsync(It.IsAny<Guid>())).ReturnsAsync(new CAHolderIndex()
        {
            CaHash = "a8ae393ecb7cba148d269c262993eacb6a1b25b4dc55270b55a9be7fc2412033",
        });

        mockUserAssetsProvider.Setup(m => m.GetCaHolderManagerInfoAsync(It.IsAny<List<string>>())).ReturnsAsync(
            new CAHolderInfo
            {
                CaHolderManagerInfo = new List<Manager>()
            });
        
        return mockUserAssetsProvider.Object;
    }
    
    private IGuardianProvider GetMockGuardianProvider()
    {
        var mockGuardianProvider = new Mock<IGuardianProvider>();
        mockGuardianProvider.Setup(m => m.GetGuardiansAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(
           
            new GuardiansDto    
            {
                CaHolderInfo = new List<GuardianDto>()
                {
                    new GuardianDto
                    {
                        OriginChainId = "AELF",
                        ChainId = "AELF",
                        ManagerInfos = new List<ManagerInfoDBase>
                        {
                            new ManagerInfoDBase()
                            {
                                Address = "123",
                                ExtraData = "234"
                            }
                        },
                        GuardianList = new GuardianBaseListDto
                        {
                            Guardians = new List<GuardianInfoBase>
                            {
                                new GuardianInfoBase
                                {
                                    IsLoginGuardian = true,
                                    IdentifierHash = "MockIdentifierHash",
                                    GuardianIdentifier = "MockGuardianIdentifier",
                                    Salt = "MockSalt",
                                    Type = "3",
                                    VerifierId = "MockVerifierId",
                                }
                            }
                        }
                    }
                }
            });

        return mockGuardianProvider.Object;
    }
    
    
    private ICAAccountProvider GetMockCaAccountProvider()
    {
        var mockCaAccountProvider = new Mock<ICAAccountProvider>();
        
        mockCaAccountProvider.Setup(m => m.GetIdentifiersAsync(It.IsAny<string>())).ReturnsAsync(new GuardianIndex
        {
            Id = "MockId",
            IdentifierHash = "MockIdentifierHash",
            Identifier = "MockIdentifier",
            CreateTime = DateTime.Now,
            Salt = "MockSalt",
            OriginalIdentifier = "MockOriginalIdentifier",
            IsDeleted = false,
        });
        
        mockCaAccountProvider
            .Setup(m => m.GetGuardianAddedCAHolderAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new GuardianAddedCAHolderDto
            {
                GuardianAddedCAHolderInfo = new GuardianAddedHolderInfo
                {
                    TotalRecordCount = 0,
                    Data = new List<GuardianResultDto>()
                }
            });


        return mockCaAccountProvider.Object;
    }

    private IAppleAuthProvider GetMockAppleAuthProvider()
    {
        var mockCaAccountProvider = new Mock<IAppleAuthProvider>();

        mockCaAccountProvider
            .Setup(m => m.VerifyAppleId(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        
        mockCaAccountProvider
            .Setup(m => m.RevokeAsync(It.IsAny<string>()))
            .ReturnsAsync(true);


        return mockCaAccountProvider.Object;
    }

    private IContractProvider GetMockContractProvider()
    {
        var mockContractProvider = new Mock<IContractProvider>();
        mockContractProvider.Setup(m => m.GetHolderInfoAsync(It.IsAny<Hash>(), It.IsAny<Hash>(), "AELF"))
            .ReturnsAsync(
                new GetHolderInfoOutput
                {
                    CreateChainId = 9992731,
                    CaHash = Hash.LoadFromHex("a8ae393ecb7cba148d269c262993eacb6a1b25b4dc55270b55a9be7fc2412033"),
                    GuardianList = new GuardianList()
                    {
                        Guardians =
                        {
                            new Portkey.Contracts.CA.Guardian
                            {
                                IsLoginGuardian = true,
                                IdentifierHash = Hash.LoadFromHex("0d12a5264e650c245c647738c27f30a75efc5b74e87c85e46fc29dd5d2bc9fe4"),
                                Salt = "MockSalt",
                                VerifierId = Hash.LoadFromHex("dbf9d0dedaec2474f89c096ff75c01dc5cdbce0e21567252e99a7f4a88428ae8"),
                                Type = GuardianType.OfEmail
                            }
                            
                        }
                    }
                    
                }

            );

        return mockContractProvider.Object;
    }
    
    private IOptionsSnapshot<ManagerCountLimitOptions> GetMockManagerCountLimitOptions()
    {
        var mockOptions = new Mock<IOptionsSnapshot<ManagerCountLimitOptions>>();

        mockOptions.Setup(o => o.Value).Returns(
            new ManagerCountLimitOptions()
            {
                Limit = 1
            });
           
        return mockOptions.Object;
    }

    
    
    
   
    
}