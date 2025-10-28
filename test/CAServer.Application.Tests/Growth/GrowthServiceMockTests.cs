using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using CAServer.CAActivity.Provider;
using CAServer.Cache;
using CAServer.Common;
using CAServer.Entities.Es;
using CAServer.Growth.Dtos;
using CAServer.Growth.Provider;
using CAServer.Guardian.Provider;
using CAServer.UserAssets.Provider;
using Moq;
using NSubstitute.Extensions;
using NSubstitute.ReturnsExtensions;
using Portkey.Contracts.CA;
using StackExchange.Redis;

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
            CaHash = "MockCaHash",
            Avatar = "MockAvatar",
            NickName = "MockNickName"
        });


        return provider.Object;
    }

    public IGrowthProvider MockGrowthProvider()
    {
        var provider = new Mock<IGrowthProvider>();
        provider.Setup(t =>
                t.GetReferralRecordListAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                    It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<List<int>>()))
            .ReturnsAsync(new List<ReferralRecordIndex>()
            {
                new ReferralRecordIndex()
                {
                    CaHash = "MockCaHash",
                    ReferralAddress = "MockAddress",
                    ReferralCaHash = "MockReferralCaHash",
                    ReferralDate = new DateTime().ToUniversalTime(),
                    ReferralCode = "MockCode",
                    IsDirectlyInvite = 1
                }
            });

        provider.Setup(t => t.GetGrowthInfoByCaHashAsync(It.IsAny<string>())).ReturnsAsync(new GrowthIndex()
        {
            CaHash = "MockCaHash",
            ReferralCode = "MockReferralCode",
            InviteCode = "MockInviteCode"
        });

        var list = new List<IndexerReferralInfo>()
        {
            new IndexerReferralInfo()
            {
                CaHash = "MockCaHash",
                ReferralCode = "MockInviteCode",
            }
        };
        provider.Setup(m => m.GetReferralInfoAsync(It.IsAny<List<string>>(), It.IsAny<List<string>>(),
            It.IsAny<List<string>>(), It.IsAny<long>(), It.IsAny<long>())).ReturnsAsync(new ReferralInfoDto()
        {
            ReferralInfo = list
        });


        provider.Setup(t => t.GetGrowthInfosAsync(It.IsAny<List<string>>(), It.IsAny<List<string>>())).ReturnsAsync(
            new List<GrowthIndex>()
            {
                new GrowthIndex()
                {
                    CaHash = "MockCaHash",
                    InviteCode = "MockInviteCode",
                    ReferralCode = "MockReferralCode"
                }
            });


        provider.Setup(t => t.AddReferralRecordAsync(It.IsAny<ReferralRecordIndex>())).ReturnsAsync(true);

        provider.Setup(t => t.GetAllGrowthInfosAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(
            (int skip, int limit) => skip == 100
                ? new List<GrowthIndex>()
                {
                    new GrowthIndex()
                    {
                        CaHash = "MockCaHash",
                        InviteCode = "MockInviteCode",
                        ReferralCode = "MockReferralCode"
                    }
                }
                : new List<GrowthIndex>());


        return provider.Object;
    }

    public IActivityProvider MockIActivityProvider()
    {
        var provider = new Mock<IActivityProvider>();
        provider.Setup(t =>
                t.GetCaHolderInfoAsync(It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new GuardiansDto()
            {
                CaHolderInfo = new List<GuardianDto>()
                {
                    new GuardianDto()
                    {
                        CaHash = "MockCaHash",
                        CaAddress = "MockCaAddress"
                    }
                }
            });
        return provider.Object;
    }

    // public ICacheProvider MockCacheProvider()
    // {
    //     var entries = new SortedSetEntry[1];
    //
    //     var provider = new Mock<ICacheProvider>();
    //     provider.Setup(t => t.GetSortedSetLengthAsync(It.IsAny<string>())).ReturnsAsync(1);
    //
    //     // provider.Setup(t => t.AddScoreAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>()))
    //     //     .ReturnsForAll();
    //
    //     provider.Setup(t => t.GetRankAsync(It.IsAny<string>(), It.IsAny<string>(), true))
    //         .ReturnsAsync(1);
    //
    //     provider.Setup(t => t.GetScoreAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(1);
    //
    //     provider.Setup(t => t.GetTopAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), true))
    //         .ReturnsAsync(entries);
    //
    //
    //     return provider.Object;
    // }
}