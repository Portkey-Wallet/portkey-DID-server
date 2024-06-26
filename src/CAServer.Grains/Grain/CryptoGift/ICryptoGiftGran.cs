using CAServer.RedPackage.Dtos;
using Orleans;

namespace CAServer.Grains.Grain.CryptoGift;

public interface ICryptoGiftGran : IGrainWithGuidKey
{
    public Task<GrainResultDto<CryptoGiftDto>> GetCryptoGift(Guid redPackageId);

    public Task<GrainResultDto<CryptoGiftDto>> CreateCryptoGift(SendRedPackageInputDto input,
        List<BucketItemDto> bucketNotClaimed, List<BucketItemDto> bucketClaimed, Guid senderId);
    
    public Task<GrainResultDto<CryptoGiftDto>> UpdateCryptoGift(CryptoGiftDto cryptoGiftDto);
}