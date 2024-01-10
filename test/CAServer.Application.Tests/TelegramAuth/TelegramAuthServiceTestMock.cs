using System.Collections.Generic;
using AElf.Types;
using CAServer.Common;
using CAServer.Telegram.Options;
using Microsoft.Extensions.Options;
using Moq;
using Portkey.Contracts.CA;

namespace CAServer.TelegramAuth;

public partial class TelegramAuthServiceTests
{
    private IOptionsSnapshot<TelegramAuthOptions> MockTelegramAuthOptionsSnapshot()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<TelegramAuthOptions>>();

        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new TelegramAuthOptions
            {
                BotName = "sTestBBot",
                RedirectUrl = new Dictionary<string, string>()
                {
                    { "portkey", "XXX" },
                    { "openlogin", "XXX" }
                }
            });
        return mockOptionsSnapshot.Object;
    }
    
    private IContractProvider MockContractProvider()
    {
        var mockContractProvider = new Mock<IContractProvider>();
        mockContractProvider.Setup(o => o.GetVerifierServersListAsync(It.IsAny<string>()))
            .ReturnsAsync(new GetVerifierServersOutput()
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
                });
        return mockContractProvider.Object;
    }
}