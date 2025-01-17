using CAServer.RedPackage.Dtos;

namespace CAServer.Grains.Grain.CryptoGift;

public interface ICryptoGiftGrain : IGrainWithGuidKey
{
    public Task<GrainResultDto<CryptoGiftDto>> GetCryptoGift(Guid redPackageId);

    public Task<GrainResultDto<CryptoGiftDto>> CreateCryptoGift(SendRedPackageInputDto input,
        List<BucketItemDto> bucketNotClaimed, List<BucketItemDto> bucketClaimed, Guid senderId);
    
    public Task<GrainResultDto<CryptoGiftDto>> UpdateCryptoGift(CryptoGiftDto cryptoGiftDto);
    
    public Task<GrainResultDto<CryptoGiftDto>> GrabCryptoGift(string identityCode, string ipAddress, int decimalForItem);
}