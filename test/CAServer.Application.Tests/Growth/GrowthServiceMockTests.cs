using System;
using System.Collections.Generic;
using AElf;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Growth.Provider;
using CAServer.UserAssets.Provider;
using Moq;
using Portkey.Contracts.CA;

namespace CAServer.Growth;

public partial class GrowthServiceTest
{
    public IContractProvider GetContractProviderMock()
    {
        var provider = new Mock<IContractProvider>();

        provider.Setup(t => t.GetVerifierServersListAsync(It.IsAny<string>()))
            .ReturnsAsync(new GetVerifierServersOutput()
            {
                VerifierServers =
                {
                    new Portkey.Contracts.CA.VerifierServer
                    {
                        Id = HashHelper.ComputeFrom("123"),
                        ImageUrl = "http://localhost:8000",
                        Name = "MockName"
                    }
                }
            });

        return provider.Object;
    }

    public IUserAssetsProvider MockUserAssetsProvider()
    {
        var provider = new Mock<IUserAssetsProvider>();
        provider.Setup(t => t.GetCaHolderIndexAsync(It.IsAny<Guid>())).ReturnsAsync(new CAHolderIndex()
        {
            CaHash = "",
            Avatar = "",
            NickName = ""
        });
        return provider.Object;
    }

    public IGrowthProvider MockGrowthProvider()
    {
        var provider = new Mock<IGrowthProvider>();
        provider.Setup(t =>
                t.GetReferralRecordListAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<ReferralRecordIndex>()
            {
                new ReferralRecordIndex()
                {
                    CaHash = "",
                    ReferralAddress = "",
                    ReferralCaHash = "",
                    ReferralDate = new DateTime().ToUniversalTime(),
                    ReferralCode = "",
                    IsDirectlyInvite = 1
                }
            });
        return provider.Object;
    }
}