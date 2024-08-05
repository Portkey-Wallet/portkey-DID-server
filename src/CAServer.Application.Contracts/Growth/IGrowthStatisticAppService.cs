using System.Threading.Tasks;
using CAServer.Growth.Dtos;

namespace CAServer.Growth;

public interface IGrowthStatisticAppService
{
    Task<ReferralResponseDto> GetReferralInfoAsync(ReferralRequestDto input);
    Task<int> GetReferralTotalCountAsync(ReferralRecordRequestDto input);
    Task CalculateReferralRankAsync();
    Task<ReferralRecordResponseDto> GetReferralRecordList(ReferralRecordRequestDto input);
    Task InitReferralRankAsync();
    Task<ReferralRecordsRankResponseDto> GetReferralRecordRankAsync(ReferralRecordRankRequestDto input);
    Task RepairHamsterDataAsync();
}