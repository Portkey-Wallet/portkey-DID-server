using CAServer.Common;
using CAServer.Settings;
using Microsoft.Extensions.Options;
using Moq;
using Portkey.Contracts.CA;

namespace CAServer.VerifierServer;

public partial class GetVerifierServerProviderTest
{
    private IOptionsSnapshot<AdaptableVariableOptions> GetAdaptableVariableOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<AdaptableVariableOptions>>();
        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new AdaptableVariableOptions
            {
                HttpConnectTimeOut = 5,
                VerifierServerExpireTime = 1000
            });
        return mockOptionsSnapshot.Object;
    }

    private IContractProvider GetMockContractProvider()
    {
        var mockContractProvider = new Mock<IContractProvider>();
        // mockContractProvider.Setup(o => o.GetVerifierServersListAsync(It.IsAny<string>()))
        // .ReturnsAsync((string chainId) => chainId == DefaultChainId ? new GetVerifierServersOutput() : null);
        return mockContractProvider.Object;
    }
}