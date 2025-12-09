using System.Threading.Tasks;
using CAServer.EnumType;
using CAServer.Growth.Dtos;

namespace CAServer.Growth;

public interface IGrowthStatisticAppService
{
    Task<ReferralResponseDto> GetReferralInfoAsync(ReferralRequestDto input);
    Task CalculateReferralRankAsync();
    Task<ReferralRecordResponseDto> GetReferralRecordList(ReferralRecordRequestDto input);
    Task<ReferralRecordsRankResponseDto> GetReferralRecordRankAsync(ReferralRecordRankRequestDto input);
    Task CalculateHamsterDataAsync();
    Task<RewardProgressResponseDto> GetRewardProgressAsync(ActivityEnums activityEnum);
    Task<BeInvitedConfigResponseDto> GetBeInvitedConfigAsync();
    Task<ActivityBaseInfoDto> ActivityBaseInfoAsync();
    
    Task<ValidateHamsterScoreResponseDto> ValidateHamsterScoreAsync(string userId);
    Task RepairHamsterDataAsync();
    Task TonGiftsValidateAsync();
    Task<GetGrowthInfosDto> GetGrowthInfosAsync(GetGrowthInfosRequestDto input);
}