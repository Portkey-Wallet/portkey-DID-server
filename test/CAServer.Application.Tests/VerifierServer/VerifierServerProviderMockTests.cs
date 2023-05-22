using CAServer.Common;
using CAServer.Settings;
using Microsoft.Extensions.Options;
using Moq;
using Portkey.Contracts.CA;

namespace CAServer.VerifierServer;

public partial class GetVerifierServerProviderTest
{
    private IOptions<AdaptableVariableOptions> GetAdaptableVariableOptions()
    {
        return new OptionsWrapper<AdaptableVariableOptions>(
            new AdaptableVariableOptions
            {
                HttpConnectTimeOut = 5,
                VerifierServerExpireTime = 1000
            });
    }

    private IContractProvider GetMockContractProvider()
    {
        var mockContractProvider = new Mock<IContractProvider>();
        // mockContractProvider.Setup(o => o.GetVerifierServersListAsync(It.IsAny<string>()))
        // .ReturnsAsync((string chainId) => chainId == DefaultChainId ? new GetVerifierServersOutput() : null);
        return mockContractProvider.Object;
    }
}