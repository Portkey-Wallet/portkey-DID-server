using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.MultiToken;
using AElf.Contracts.MultiToken;
using CAServer.Cache;
using CAServer.ClaimToken.Dtos;
using CAServer.Common;
using CAServer.Hub;
using CAServer.Options;
using Microsoft.Extensions.Options;
using Moq;
using Portkey.Contracts.CA;
using GetBalanceOutput = AElf.Contracts.MultiToken.GetBalanceOutput;

namespace CAServer.ClaimToken;

public partial class ClaimTokenTests
{
    private ICacheProvider GetMockCacheProvider()
    {
        return new MockCacheProvider();
    }

    private IOptionsSnapshot<ClaimTokenInfoOptions> GetClaimTokenInfoOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<ClaimTokenInfoOptions>>();
        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new ClaimTokenInfoOptions
            {
                ChainId = "mockChainId",
                PublicKey = "mockPrivateKey",
                ClaimTokenAddress = "mockClaimTokenAddress",
                ClaimTokenAmount = 100,
                GetClaimTokenLimit = 0
            });
        return mockOptionsSnapshot.Object;
    }

    private IOptionsSnapshot<ClaimTokenWhiteListAddressesOptions> GetClaimTokenWhiteListAddressesOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<ClaimTokenWhiteListAddressesOptions>>();
        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new ClaimTokenWhiteListAddressesOptions
            {
                WhiteListAddresses = new List<string>
                {
                    "MockAddress"
                }
            });
        return mockOptionsSnapshot.Object;
    }

    private IContractProvider GetMockContractProvider()
    {
        var mockContractProvider = new Mock<IContractProvider>();
        mockContractProvider.Setup(o => o.GetBalanceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string chainId, string address, string symbol) => chainId == "mockChainId"
                ? new GetBalanceOutput() { Balance = 80 }
                : new GetBalanceOutput() { Balance = 120 });

        mockContractProvider.Setup(o => o.ClaimTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        mockContractProvider.Setup(o =>
                o.SendTransferAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new SendTransactionOutput() { TransactionId = "" });

        return mockContractProvider.Object;
    }
}