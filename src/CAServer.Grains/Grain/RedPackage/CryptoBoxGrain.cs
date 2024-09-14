using CAServer.EnumType;
using CAServer.Grains.State;
using CAServer.Grains.State.RedPackage;
using CAServer.RedPackage;
using CAServer.RedPackage.Dtos;
using Orleans.Providers.Streams.Generator;
using Serilog;
using Serilog.Core;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.RedPackage;

public class CryptoBoxGrain : Orleans.Grain<RedPackageState>, ICryptoBoxGrain
{
    private readonly IObjectMapper _objectMapper;

    private const int RePackagePlaceMove = 3;

    public CryptoBoxGrain(IObjectMapper objectMapper)
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

    public async Task<GrainResultDto<RedPackageDetailDto>> CreateRedPackage(SendRedPackageInputDto input, int decimalIn,
        long minAmount,
        Guid senderId, long expireTimeMs)
    {
        var result = new GrainResultDto<RedPackageDetailDto>();
        if (State.Status != RedPackageStatus.Init)
        {
            result.Success = false;
            result.Message = "RedPackage has been created";
            return result;
        }

        var bucketResult = GenerateBucket(input.Count, long.Parse(input.TotalAmount), minAmount, decimalIn, input.Type);
        State = _objectMapper.Map<SendRedPackageInputDto, RedPackageState>(input);
        State.SessionId = input.SessionId;
        State.Status = RedPackageStatus.NotClaimed;
        State.CreateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        State.EndTime = 0;
        State.ExpireTime = State.CreateTime + expireTimeMs;
        State.Decimal = decimalIn;
        State.BucketNotClaimed = bucketResult.Item1;
        State.BucketClaimed = new List<BucketItem>();
        State.Items = new List<GrabItem>();
        State.IfRefund = false;
        State.SenderId = senderId;
        
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<RedPackageState, RedPackageDetailDto>(State);
        return result;
    }

    public Task<GrainResultDto<RedPackageDetailDto>> GetRedPackage(int skip, int max, Guid userId, RedPackageDisplayType displayType)
    {
        var result = new GrainResultDto<RedPackageDetailDto>();
        result.Success = true;
        var dto = _objectMapper.Map<RedPackageState, RedPackageDetailDto>(State);
        dto.TotalCount = State.Items.Count;
        dto.IsCurrentUserGrabbed = State.Items.Any(item => item.UserId == userId);
        if (RedPackageDisplayType.Common.Equals(displayType))
        {
            dto.Items = _objectMapper.Map<List<GrabItem>, List<GrabItemDto>>(State.Items.Skip(skip).Take(max).ToList());
        }
        else 
        {
            //crypto gift type should be paged with the pre grabbed crypto gift items
            dto.Items = _objectMapper.Map<List<GrabItem>, List<GrabItemDto>>(State.Items.ToList());
        }
        result.Data = dto;
        return Task.FromResult(result);
    }

    public async Task<GrainResultDto<bool>> ExpireRedPackage()
    {
        var result = new GrainResultDto<bool>();
        result.Success = true;
        result.Data = true;
        if (State.Status == RedPackageStatus.Cancelled
            || State.Status == RedPackageStatus.FullyClaimed)
        {
            return result;
        }

        State.Status = RedPackageStatus.Expired;

        await WriteStateAsync();
        return result;
    }

    public async Task<GrainResultDto<bool>> CancelRedPackage()
    {
        var result = new GrainResultDto<bool>();
        result.Success = true;
        result.Data = true;
        State.Status = RedPackageStatus.Cancelled;
        await WriteStateAsync();
        return result;
    }

    public async Task<GrainResultDto<GrabResultDto>> GrabRedPackage(Guid userId, string caAddress)
    {
        var result = new GrainResultDto<GrabResultDto>();
        var checkResult = CheckRedPackagePermissions(userId);
        if (checkResult.Item1 == false)
        {
            result.Success = false;
            result.Data = new GrabResultDto()
            {
                Result = RedPackageGrabStatus.Fail,
                ErrorMessage = checkResult.Item2,
                Status = State.Status,
                ExpireTime = State.ExpireTime
            };
            if (checkResult.Item2.Equals(RedPackageConsts.RedPackageUserGrabbed))
            {
                var grabed = State.Items.First(item => item.UserId == userId);
                result.Data.Amount = grabed.Amount.ToString();
                result.Data.Decimal = grabed.Decimal;
            }

            return result;
        }

        if (State.Status == RedPackageStatus.NotClaimed)
        {
            State.Status = RedPackageStatus.Claimed;
        }

        var bucket = GetBucket(userId);
        var grabItem = new GrabItem()
        {
            Amount = bucket.Amount,
            Decimal = State.Decimal,
            PaymentCompleted = false,
            GrabTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            IsLuckyKing = State.Type == RedPackageType.Random && bucket.IsLuckyKing,
            UserId = userId,
            CaAddress = caAddress
        };
        if (grabItem.IsLuckyKing)
        {
            State.LuckKingId = userId;
        }

        State.Items.Add(grabItem);
        State.GrabbedAmount += bucket.Amount;
        State.Grabbed += 1;
        if (State.Grabbed == State.Count)
        {
            State.Status = RedPackageStatus.FullyClaimed;
            State.EndTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        result.Success = true;
        result.Data = new GrabResultDto()
        {
            Result = RedPackageGrabStatus.Success,
            ErrorMessage = "",
            Amount = bucket.Amount.ToString(),
            Decimal = State.Decimal,
            Status = State.Status,
            BucketItem = _objectMapper.Map<BucketItem, BucketItemDto>(bucket)
        };

        await WriteStateAsync();
        return result;
    }

    public async Task<GrainResultDto<GrabResultDto>> GrabRedPackageWithIdentityInfo(Guid userId, string caAddress, string ipAddress, string identity)
    {
        var result = new GrainResultDto<GrabResultDto>();
        var checkResult = CheckRedPackagePermissions(userId);
        if (checkResult.Item1 == false)
        {
            result.Success = false;
            result.Data = new GrabResultDto()
            {
                Result = RedPackageGrabStatus.Fail,
                ErrorMessage = checkResult.Item2,
                Status = State.Status,
                ExpireTime = State.ExpireTime
            };
            if (checkResult.Item2.Equals(RedPackageConsts.RedPackageUserGrabbed))
            {
                var grabed = State.Items.First(item => item.UserId == userId);
                result.Data.Amount = grabed.Amount.ToString();
                result.Data.Decimal = grabed.Decimal;
            }

            return result;
        }

        if (State.Status == RedPackageStatus.NotClaimed)
        {
            State.Status = RedPackageStatus.Claimed;
        }

        var bucket = GetBucket(userId);
        var grabItem = new GrabItem()
        {
            Amount = bucket.Amount,
            Decimal = State.Decimal,
            PaymentCompleted = false,
            GrabTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            IsLuckyKing = State.Type == RedPackageType.Random && bucket.IsLuckyKing,
            UserId = userId,
            CaAddress = caAddress,
            IpAddress = ipAddress,
            Identity = identity
        };
        if (grabItem.IsLuckyKing)
        {
            State.LuckKingId = userId;
        }

        State.Items.Add(grabItem);
        State.GrabbedAmount += bucket.Amount;
        State.Grabbed += 1;
        if (State.Grabbed == State.Count)
        {
            State.Status = RedPackageStatus.FullyClaimed;
            State.EndTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        result.Success = true;
        result.Data = new GrabResultDto()
        {
            Result = RedPackageGrabStatus.Success,
            ErrorMessage = "",
            Amount = bucket.Amount.ToString(),
            Decimal = State.Decimal,
            Status = State.Status,
            BucketItem = _objectMapper.Map<BucketItem, BucketItemDto>(bucket)
        };

        await WriteStateAsync();
        return result;
    }

    public async Task<GrainResultDto<GrabResultDto>> CryptoGiftTransferToRedPackage(Guid userId, string caAddress, PreGrabBucketItemDto preGrabBucketItemDto,
        string ipAddress, string identity)
    {
        var result = new GrainResultDto<GrabResultDto>();
        var checkResult = AutoTransferWrappedCheckRedPackagePermissions(userId, preGrabBucketItemDto);
        if (checkResult.Item1 == false)
        {
            result.Success = false;
            result.Data = new GrabResultDto()
            {
                Result = RedPackageGrabStatus.Fail,
                ErrorMessage = checkResult.Item2,
                Status = State.Status,
                ExpireTime = State.ExpireTime
            };
            if (checkResult.Item2.Equals(RedPackageConsts.RedPackageUserGrabbed))
            {
                var grabed = State.Items.First(item => item.UserId == userId);
                result.Data.Amount = grabed.Amount.ToString();
                result.Data.Decimal = grabed.Decimal;
            }

            return result;
        }

        if (State.Status == RedPackageStatus.NotClaimed)
        {
            State.Status = RedPackageStatus.Claimed;
        }

        var bucket = GetBucketByIndex(userId, preGrabBucketItemDto);
        
        var grabItem = new GrabItem()
        {
            Amount = bucket.Amount,
            Decimal = State.Decimal,
            PaymentCompleted = false,
            GrabTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            IsLuckyKing = State.Type == RedPackageType.Random && bucket.IsLuckyKing,
            UserId = userId,
            CaAddress = caAddress,
            IpAddress = ipAddress,
            Identity = identity
        };
        if (grabItem.IsLuckyKing)
        {
            State.LuckKingId = userId;
        }

        State.Items.Add(grabItem);
        State.GrabbedAmount += bucket.Amount;
        State.Grabbed += 1;
        if (State.Grabbed == State.Count)
        {
            State.Status = RedPackageStatus.FullyClaimed;
            State.EndTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        result.Success = true;
        result.Data = new GrabResultDto()
        {
            Result = RedPackageGrabStatus.Success,
            ErrorMessage = "",
            Amount = bucket.Amount.ToString(),
            Decimal = State.Decimal,
            Status = State.Status
        };

        await WriteStateAsync();
        return result;
    }

    public async Task<GrainResultDto<RedPackageDetailDto>> UpdateRedPackage(List<GrabItemDto> grabItemDtos)
    {
        var result = new GrainResultDto<RedPackageDetailDto>();
        if (State == null || State.Items.IsNullOrEmpty())
        {
            result.Success = false;
            return result;
        }

        foreach (var grabItemDto in grabItemDtos.Where(o => !o.PaymentCompleted))
        {
            _ = State.Items
                .Where(item => item.UserId.Equals(grabItemDto.UserId) && grabItemDto.CaAddress.Equals(item.CaAddress))
                .First(item => item.PaymentCompleted = true);
        }

        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<RedPackageState, RedPackageDetailDto>(State);
        return result;
    }

    public Task<GrainResultDto<RedPackageDetailDto>> GetRedPackage(Guid packageId)
    {
        var result = new GrainResultDto<RedPackageDetailDto>();
        if (State?.Id == null || !packageId.Equals(State.Id))
        {
            result.Success = false;
            result.Message = "there is no record";
            return Task.FromResult(result);
        }
        result.Success = true;
        var dto = _objectMapper.Map<RedPackageState, RedPackageDetailDto>(State);
        dto.TotalCount = State.Items.Count;
        dto.Items = _objectMapper.Map<List<GrabItem>, List<GrabItemDto>>(State.Items);
        result.Data = dto;
        return Task.FromResult(result);
    }

    public async Task<GrainResultDto<bool>> UpdateRedPackageExpire()
    {
        var result = new GrainResultDto<bool>();
        result.Success = true;
        result.Data = true;
        State.IfRefund = true;
        await WriteStateAsync();
        return result;
    }

    private (bool, string) AutoTransferWrappedCheckRedPackagePermissions(Guid userId, PreGrabBucketItemDto preGrabBucketItemDto)
    {
        var checkResult = CheckRedPackagePermissions(userId);
        if (checkResult.Item1)
        {
            if (State.BucketNotClaimed.Any(bucket => bucket.Index.Equals(preGrabBucketItemDto.Index))
                && !State.BucketClaimed.Any(bucket => bucket.Index.Equals(preGrabBucketItemDto.Index)))
            {
                return (true, "");
            }
            else
            {
                return (false, RedPackageConsts.RedPackageGrabbedByOthers);
            }
        }

        return checkResult;
    }
    
    private (bool, string) CheckRedPackagePermissions(Guid userId)
    {
        if (State.Items.Any(item => item.UserId == userId))
        {
            return (false, RedPackageConsts.RedPackageUserGrabbed);
        }

        if (State.Status == RedPackageStatus.Expired)
        {
            return (false, RedPackageConsts.RedPackageExpired);
        }

        if (State.Status == RedPackageStatus.Cancelled)
        {
            return (false, RedPackageConsts.RedPackageCancelled);
        }

        if (State.Status == RedPackageStatus.FullyClaimed || State.Grabbed == State.Count ||
            State.BucketNotClaimed.Count == 0)
        {
            return (false, RedPackageConsts.RedPackageFullyClaimed);
        }

        if (State.Status == RedPackageStatus.Init)
        {
            return (false, RedPackageConsts.RedPackageNotSet);
        }

        return (true, "");
    }

    private BucketItem GetBucket(Guid userId)
    {
        var random = new Random();
        var index = random.Next(State.BucketNotClaimed.Count);
        var bucket = State.BucketNotClaimed[index];
        if (index != 0 && State.BucketNotClaimed[0].IsLuckyKing)
        {
            if (bucket.Amount.CompareTo(State.BucketNotClaimed[0].Amount) == 0)
            {
                bucket.IsLuckyKing = true;
                State.BucketNotClaimed[0].IsLuckyKing = false;
            }
        }

        bucket.UserId = userId;
        State.BucketNotClaimed.Remove(bucket);
        State.BucketClaimed.Add(bucket);
        return bucket;
    }
    
    private BucketItem GetBucketByIndex(Guid userId, PreGrabBucketItemDto preGrabBucketItemDto)
    {
        var index = preGrabBucketItemDto.Index;
        var bucket = State.BucketNotClaimed.FirstOrDefault(bucket => bucket.Index.Equals(index));
        if (bucket == null)
        {
            throw new UserFriendlyException("CryptoGiftTransferToRedPackage GetBucketByIndex Failed");
        }
        if (index != 0 && State.BucketNotClaimed[0].IsLuckyKing)
        {
            if (bucket.Amount.CompareTo(State.BucketNotClaimed[0].Amount) == 0)
            {
                bucket.IsLuckyKing = true;
                State.BucketNotClaimed[0].IsLuckyKing = false;
            }
        }

        bucket.UserId = userId;
        State.BucketNotClaimed.Remove(bucket);
        State.BucketClaimed.Add(bucket);
        return bucket;
    }

    public (List<BucketItem>, int) GenerateBucket(int count, long totalAmount, long minAmount, int decimalIn,
        RedPackageType type)
    {
        switch (type)
        {
            case RedPackageType.Random:
                return GenerateRandomBucket(count, totalAmount, minAmount, decimalIn);
            case RedPackageType.Fixed:
                return GenerateFixBucket(count, totalAmount);
            case RedPackageType.QuickTransfer:
                return GenerateFixBucket(count, totalAmount);
        }

        return (new List<BucketItem>(), 0);
    }

    private (List<BucketItem>, int) GenerateFixBucket(int count, long totalAmount)
    {
        var avg = totalAmount / count;
        var bucket = new List<BucketItem>();
        var rest = totalAmount;
        for (var i = 0; i < count; i++)
        {
            bucket.Add(new BucketItem()
            {
                Index = i,
                Amount = avg
            });
            rest -= avg;
        }

        for (var i = 0; i < rest; i++)
        {
            bucket[i].Amount += 1;
            rest -= 1;
            if (rest <= 0)
            {
                break;
            }
        }

        bucket[count - 1].Amount += rest;
        return (bucket, 0);
    }

    private (List<BucketItem>, int) GenerateRandomBucket(int count, long totalAmount, long minAmount, int decimalIn)
    {
        var buckets = new List<BucketItem>();
        var remainAmount = totalAmount;
        int decimalPlaces = BucketRandomSpecialOperation(totalAmount, count, decimalIn);
        long realMinAmount = decimalIn == 0 ? 1 : (long)Math.Pow(10, decimalIn - decimalPlaces);

        int luckyKingIndex = 0;
        long luckyKingAmount = realMinAmount;

        for (var i = 0; i < count; i++)
        {
            buckets.Add(new BucketItem()
            {
                Index = i,
                Amount = realMinAmount
            });
            remainAmount -= realMinAmount;
        }

        Random random = new Random();
        for (var i = 0; i < count; i++)
        {
            if (remainAmount <= 0)
            {
                break;
            }

            long allocationAmount;
            if (i == count - 1)
            {
                allocationAmount = remainAmount;
            }
            else
            {
                long maxAllocationAmount = remainAmount / (count - i) * 2;
                double randomNumber = random.NextDouble();
                allocationAmount = (long)(randomNumber * maxAllocationAmount) / realMinAmount * realMinAmount;
            }

            buckets[i].Amount += allocationAmount;

            if (buckets[i].Amount > luckyKingAmount)
            {
                luckyKingAmount = buckets[i].Amount;
                luckyKingIndex = i;
            }

            remainAmount -= allocationAmount;
        }

        buckets[luckyKingIndex].IsLuckyKing = true;

        buckets.Sort((item1, item2) => item2.Amount.CompareTo(item1.Amount));

        return (buckets, luckyKingIndex);
    }

    private int BucketRandomSpecialOperation(long total, int count, int decimalIn)
    {
        // little point in 0~2 
        int places = DecimalPlaces(total, decimalIn);
        if (places < RePackagePlaceMove)
        {
            places += 2;
        }

        long baseJudgeCount = (long)Math.Pow(10, decimalIn - places);
        if (baseJudgeCount * count > total)
        {
            return decimalIn;
        }

        return places;
    }

    private int DecimalPlaces(long count, int decimalIn)
    {
        long baseJudgeCount = (long)Math.Pow(10, decimalIn);
        decimal tempNum = (decimal)count / baseJudgeCount;
        int[] bits = decimal.GetBits(tempNum);
        int exponent = (bits[3] >> 16) & 0x1F;
        return exponent;
    }
}