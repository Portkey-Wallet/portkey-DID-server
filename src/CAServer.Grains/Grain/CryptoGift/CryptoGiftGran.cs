using CAServer.Grains.State;
using CAServer.RedPackage.Dtos;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.CryptoGift;

public class CryptoGiftGran : Orleans.Grain<CryptoGiftState>, ICryptoGiftGran
{
    private readonly IObjectMapper _objectMapper;

    public CryptoGiftGran(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }
    
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<GrainResultDto<CryptoGiftDto>> GetCryptoGift(Guid redPackageId)
    {
        var result = new GrainResultDto<CryptoGiftDto>();
        if (redPackageId.Equals(State.Id))
        {
            result.Success = true;
            result.Data = _objectMapper.Map<CryptoGiftState, CryptoGiftDto>(State);
            return result;
        }
        result.Success = false;
        result.Message = "there is no record";
        return result;
    }

    public async Task<GrainResultDto<CryptoGiftDto>> CreateCryptoGift(SendRedPackageInputDto input,
        List<BucketItemDto> bucketNotClaimed, List<BucketItemDto> bucketClaimed, Guid senderId)
    {
        var result = new GrainResultDto<CryptoGiftDto>();
        if (State.Id.Equals(input.Id))
        {
            result.Success = false;
            result.Message = "CryptoGift has been existed";
            return result;
        }

        State = _objectMapper.Map<SendRedPackageInputDto, CryptoGiftState>(input);
        State.PreGrabbedAmount = 0;
        State.SenderId = senderId;
        State.CreateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        State.Symbol = input.Symbol;
        State.Items = new List<PreGrabItem>();
        State.BucketClaimed = _objectMapper.Map<List<BucketItemDto>, List<PreGrabBucketItemDto>>(bucketClaimed);
        var preGrabBucketNotClaimed = _objectMapper.Map<List<BucketItemDto>, List<PreGrabBucketItemDto>>(bucketNotClaimed);
        for (var i = 0; i < preGrabBucketNotClaimed.Count; i++)
        {
            preGrabBucketNotClaimed[i].Index = i;
        }
        State.BucketNotClaimed = preGrabBucketNotClaimed;
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<CryptoGiftState, CryptoGiftDto>(State);
        return result;
    }

    public async Task<GrainResultDto<CryptoGiftDto>> UpdateCryptoGift(CryptoGiftDto cryptoGiftDto)
    {
        State = _objectMapper.Map<CryptoGiftDto, CryptoGiftState>(cryptoGiftDto);
        await WriteStateAsync();
        
        return new GrainResultDto<CryptoGiftDto>
        {
            Success = true,
            Data = cryptoGiftDto
        };
    }
}