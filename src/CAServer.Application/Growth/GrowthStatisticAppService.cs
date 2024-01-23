using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Growth.Dtos;
using CAServer.Growth.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Growth;

[RemoteService(false), DisableAuditing]
public class GrowthStatisticAppService: CAServerAppService, IGrowthStatisticAppService
{
    private readonly IGrowthProvider _growthProvider;

    public GrowthStatisticAppService(IGrowthProvider growthProvider)
    {
        _growthProvider = growthProvider;
    }
    
    // who i invited
    public async Task<ReferralResponseDto> GetReferralInfoAsync(ReferralRequestDto input)
    {
        var result = new ReferralResponseDto();

        foreach (var caHash in input.CaHashes)
        {
            result.ReferralInfos.Add(new Referral()
            {
                CaHash = caHash
            });
        }

        var growthInfos = await _growthProvider.GetGrowthInfosAsync(input.CaHashes, null);
        if (growthInfos.IsNullOrEmpty())
        {
            return result;
        }

        foreach (var growthInfo in growthInfos)
        {
            var referralInfo = result.ReferralInfos.First(t => t.CaHash == growthInfo.CaHash);
            referralInfo.ReferralCode = growthInfo.ReferralCode;
            referralInfo.ProjectCode = growthInfo.ProjectCode;
            referralInfo.InviteCode = growthInfo.InviteCode;
        }

        await GetReferralInfoListAsync(result.ReferralInfos);
        return result;
    }

    private async Task GetReferralInfoListAsync(List<Referral> referralInfos)
    {
        if (referralInfos.IsNullOrEmpty()) return;

        var caHashes = referralInfos.Select(t => t.CaHash).ToList();
        var growthInfos = await _growthProvider.GetGrowthInfosAsync(caHashes, null);
        if (growthInfos.IsNullOrEmpty()) return;

        foreach (var growthInfo in growthInfos)
        {
            var referral = referralInfos.First(t => t.CaHash == growthInfo.CaHash);
            referral.InviteCode = growthInfo.InviteCode;
        }

        var inviteCodes = growthInfos.Select(t => t.InviteCode).ToList();
        var indexerReferralInfos =
            await _growthProvider.GetReferralInfoAsync(new List<string>(), inviteCodes,
                new List<string> { MethodName.CreateCAHolder });

        if (indexerReferralInfos.ReferralInfo.IsNullOrEmpty())
        {
            return;
        }

        foreach (var referralInfo in indexerReferralInfos.ReferralInfo)
        {
            var referral = referralInfos.First(t => t.InviteCode == referralInfo.ReferralCode);
            referral.Children.Add(new Referral()
            {
                CaHash = referralInfo.CaHash,
                ProjectCode = referralInfo.ProjectCode,
                ReferralCode = referralInfo.ReferralCode
            });
        }

        var children = referralInfos.SelectMany(t => t.Children).ToList();
        await GetReferralInfoListAsync(children);
    }
}