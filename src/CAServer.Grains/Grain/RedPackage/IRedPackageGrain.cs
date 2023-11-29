using CAServer.RedPackage.Dtos;
using Orleans;

namespace CAServer.Grains.Grain.RedPackage;

public interface IRedPackageGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<RedPackageDetailDto>> CreateRedPackage(SendRedPackageInputDto input, int decimalIn, long minAmount,
        Guid senderId);
    Task<GrainResultDto<RedPackageDetailDto>> GetRedPackage(int skip, int max,Guid userId);
    Task<GrainResultDto<bool>> ExpireRedPackage();
    Task<GrainResultDto<bool>> CancelRedPackage();
    Task<GrainResultDto<GrabResultDto>> GrabRedPackage(Guid userId,string caAddress);

    Task<GrainResultDto<bool>> UpdateRedPackage(List<GrabItemDto> grabItemDtos);
    
    Task<GrainResultDto<RedPackageDetailDto>> GetRedPackage(Guid packageId);
    Task<GrainResultDto<bool>> UpdateRedPackageExpire();

}