using AElf.Types;
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
        mockContractProvider.Setup(o => o.GetVerifierServersListAsync(It.IsAny<string>()))
            .ReturnsAsync((string chainId) => chainId == DefaultChainId
                ? new GetVerifierServersOutput()
                {
                    VerifierServers =
                    {
                        new Portkey.Contracts.CA.VerifierServer()
                        {
                            Id = Hash.LoadFromHex("50986afa3095f66bd590d6ab26218cc2ed2ef4b1f6e7cdab5b3cbb2cd8a540f8"),
                            EndPoints =
                            {
                                "http://127.0.0.1:1122"
                            },
                            VerifierAddresses =
                            {
                                Address.FromBase58("2mBnRTqXMb5Afz4CWM2QakLRVDfaq2doJNRNQT1MXoi2uc6Zy3")
                            }
                            
                        }
                    }
                }
                : null);
        return mockContractProvider.Object;
    }
}