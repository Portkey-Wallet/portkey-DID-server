// using System;
// using System.Collections.Generic;
// using CAServer.AppleAuth.Provider;
// using CAServer.CAAccount.Provider;
// using CAServer.Entities.Es;
// using CAServer.Guardian;
// using CAServer.Guardian.Provider;
// using CAServer.UserAssets;
// using CAServer.UserAssets.Provider;
// using Moq;
// using GuardianDto = CAServer.Guardian.Provider.GuardianDto;
//
// namespace CAServer.CAAccount;
//
// public partial class RevokeAccountTests
// {
//     
//     private IUserAssetsProvider GetMockUserAssetsProvider()
//     {
//         var mockUserAssetsProvider = new Mock<IUserAssetsProvider>();
//         
//         mockUserAssetsProvider.Setup(m =>
//                 m.GetUserTokenInfoAsync(It.IsAny<List<CAAddressInfo>>(), It.IsAny<string>(), It.IsAny<int>(),
//                     It.IsAny<int>()))
//             .ReturnsAsync(new IndexerTokenInfos
//             {
//                 CaHolderTokenBalanceInfo = new CaHolderTokenBalanceInfo
//                 {
//                     totalRecordCount = 0,
//                     Data = new List<IndexerTokenInfo>()
//                 }
//             });
//         
//         
//         mockUserAssetsProvider.Setup(m =>
//                 m.GetUserNftInfoAsync(It.IsAny<List<CAAddressInfo>>(), It.IsAny<string>(), It.IsAny<int>(),
//                     It.IsAny<int>()))
//             .ReturnsAsync(
//                 new IndexerNftInfos
//                 {
//                     CaHolderNFTBalanceInfo = new CaHolderNFTBalanceInfo
//                     {
//                         TotalRecordCount = 0,
//                         Data = new List<IndexerNftInfo>()
//                     }
//                 });
//         
//         mockUserAssetsProvider.Setup(m => m.GetCaHolderIndexAsync(It.IsAny<Guid>())).ReturnsAsync(new CAHolderIndex()
//         {
//             CaHash = "a8ae393ecb7cba148d269c262993eacb6a1b25b4dc55270b55a9be7fc2412033",
//         });
//
//         mockUserAssetsProvider.Setup(m => m.GetCaHolderManagerInfoAsync(It.IsAny<List<string>>())).ReturnsAsync(
//             new CAHolderInfo
//             {
//                 CaHolderManagerInfo = new List<Manager>()
//             });
//         
//         return mockUserAssetsProvider.Object;
//     }
//     
//     private IGuardianProvider GetMockGuardianProvider()
//     {
//         var mockGuardianProvider = new Mock<IGuardianProvider>();
//         mockGuardianProvider.Setup(m => m.GetGuardiansAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(
//             new GuardiansDto()
//             {
//                 CaHolderInfo = new List<GuardianDto>()
//                 {
//                     new GuardianDto
//                     {
//                         OriginChainId = "AELF",
//                         ChainId = "AELF",
//                         GuardianList = new GuardianBaseListDto
//                         {
//                             Guardians = new List<GuardianInfoBase>
//                             {
//                                 new GuardianInfoBase
//                                 {
//                                     IsLoginGuardian = true,
//                                     IdentifierHash = "MockIdentifierHash",
//                                     GuardianIdentifier = "MockGuardianIdentifier",
//                                     Salt = "MockSalt",
//                                     Type = "3",
//                                     VerifierId = "MockVerifierId",
//                                 }
//                             }
//                         }
//                     }
//                 }
//             });
//
//         return mockGuardianProvider.Object;
//     }
//     
//     
//     private ICAAccountProvider GetMockCaAccountProvider()
//     {
//         var mockCaAccountProvider = new Mock<ICAAccountProvider>();
//         
//         mockCaAccountProvider.Setup(m => m.GetIdentifiersAsync(It.IsAny<string>())).ReturnsAsync(new GuardianIndex
//         {
//             Id = "MockId",
//             IdentifierHash = "MockIdentifierHash",
//             Identifier = "MockIdentifier",
//             CreateTime = DateTime.Now,
//             Salt = "MockSalt",
//             OriginalIdentifier = "MockOriginalIdentifier",
//             IsDeleted = false,
//         });
//         
//         mockCaAccountProvider
//             .Setup(m => m.GetGuardianAddedCAHolderAsync("MockIdentifierHash", It.IsAny<int>(), It.IsAny<int>()))
//             .ReturnsAsync(new GuardianAddedCAHolderDto
//             {
//                 GuardianAddedCAHolderInfo = new GuardianAddedHolderInfo
//                 {
//                     TotalRecordCount = 0,
//                     Data = new List<Guardian.GuardianDto>
//                     {
//                         new()
//                     }
//                 }
//             });
//
//
//         return mockCaAccountProvider.Object;
//     }
//
//     private IAppleAuthProvider GetMockAppleAuthProvider()
//     {
//         var mockCaAccountProvider = new Mock<IAppleAuthProvider>();
//
//         mockCaAccountProvider
//             .Setup(m => m.VerifyAppleId(It.IsAny<string>(), It.IsAny<string>()))
//             .ReturnsAsync(true);
//         
//         mockCaAccountProvider
//             .Setup(m => m.RevokeAsync(It.IsAny<string>()))
//             .ReturnsAsync(true);
//
//
//         return mockCaAccountProvider.Object;
//     }
// }