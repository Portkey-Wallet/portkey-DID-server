using CAServer.EnumType;
using CAServer.Grains.State;
using CAServer.RedPackage.Dtos;

namespace CAServer.Grains.Grain.RedPackage;

public interface ICryptoBoxGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<RedPackageDetailDto>> CreateRedPackage(SendRedPackageInputDto input, int decimalIn,
        long minAmount,
        Guid senderId, long expireTimeMs);
    Task<GrainResultDto<RedPackageDetailDto>> GetRedPackage(int skip, int max, Guid userId, RedPackageDisplayType displayType = RedPackageDisplayType.Common);
    Task<GrainResultDto<bool>> ExpireRedPackage();
    Task<GrainResultDto<bool>> CancelRedPackage();
    Task<GrainResultDto<GrabResultDto>> GrabRedPackage(Guid userId,string caAddress);
    Task<GrainResultDto<GrabResultDto>> GrabRedPackageWithIdentityInfo(Guid userId, string caAddress, string ipAddress, string identity);

    Task<GrainResultDto<GrabResultDto>> CryptoGiftTransferToRedPackage(Guid userId, string caAddress,
        PreGrabBucketItemDto preGrabBucketItemDto, string ipAddress, string identity);

    Task<GrainResultDto<RedPackageDetailDto>> UpdateRedPackage(List<GrabItemDto> grabItemDtos);
    
    Task<GrainResultDto<RedPackageDetailDto>> GetRedPackage(Guid packageId);
    Task<GrainResultDto<bool>> UpdateRedPackageExpire();

}