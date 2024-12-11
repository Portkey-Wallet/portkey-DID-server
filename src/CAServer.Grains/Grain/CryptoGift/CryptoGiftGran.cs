using CAServer.EnumType;
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
    
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, token);
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
        State.BucketNotClaimed = _objectMapper.Map<List<BucketItemDto>, List<PreGrabBucketItemDto>>(bucketNotClaimed);
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

    public async Task<GrainResultDto<CryptoGiftDto>> GrabCryptoGift(string identityCode, string ipAddress, int decimalForItem)
    {
        if (State.BucketClaimed.Any(bucket => bucket.IdentityCode.Equals(identityCode)))
        {
            return new GrainResultDto<CryptoGiftDto>
            {
                Success = false,
                Message = "You have grabbed the crypto gift ~"
            };
        }

        if (State.BucketNotClaimed.Count == 0)
        {
            return new GrainResultDto<CryptoGiftDto>
            {
                Success = false,
                Message = "there was no quota left ~"
            };
        }
        PreGrabBucketItemDto preGrabBucketItemDto = GetBucket(identityCode);
        if (State.Items.Any(item => !GrabbedStatus.Expired.Equals(item.GrabbedStatus)
                                    && item.Index.Equals(preGrabBucketItemDto.Index)))
        {
            return new GrainResultDto<CryptoGiftDto>
            {
                Success = false,
                Message = "The crypto gift has been grabbed by others, please try again ~"
            };
        }
        State.Items.Add(new PreGrabItem()
        {
            Index = preGrabBucketItemDto.Index,
            Amount = preGrabBucketItemDto.Amount,
            Decimal = decimalForItem,
            GrabbedStatus = GrabbedStatus.Created,
            GrabTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            IpAddress = ipAddress,
            IdentityCode = identityCode
        });
        State.PreGrabbedAmount += preGrabBucketItemDto.Amount;
        
        await WriteStateAsync();
        
        return new GrainResultDto<CryptoGiftDto>
        {
            Success = true,
            Data = _objectMapper.Map<CryptoGiftState, CryptoGiftDto>(State)
        };
    }
    
    private PreGrabBucketItemDto GetBucket(string identityCode)
    {
        var random = new Random();
        var index = random.Next(State.BucketNotClaimed.Count);
        var bucket = State.BucketNotClaimed[index];
        bucket.IdentityCode = identityCode;
        State.BucketNotClaimed.Remove(bucket);
        State.BucketClaimed.Add(bucket);
        return bucket;
    }
}