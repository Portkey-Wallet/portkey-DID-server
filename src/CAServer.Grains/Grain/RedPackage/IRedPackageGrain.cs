using CAServer.RedPackage.Dtos;
using Orleans;

namespace CAServer.Grains.Grain.RedPackage;

public interface IRedPackageGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<RedPackageDetailDto>> CreateRedPackage(SendRedPackageInputDto input, int decimalIn, decimal minAmount,
        Guid senderId);
    Task<GrainResultDto<RedPackageDetailDto>> GetRedPackage(int skip, int max);
    Task<GrainResultDto<bool>> IsUserIdGrab(Guid userId);
    Task<GrainResultDto<bool>> DeleteRedPackage();
    Task<GrainResultDto<bool>> CancelRedPackage();
    Task<GrainResultDto<GrabResultDto>> GrabRedPackage(Guid userId,string caAddress);
}