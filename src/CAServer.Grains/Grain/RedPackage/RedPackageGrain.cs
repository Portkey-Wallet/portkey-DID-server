using CAServer.Grains.State.RedPackage;
using CAServer.RedPackage;
using CAServer.RedPackage.Dtos;
using Orleans.Providers.Streams.Generator;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.RedPackage;

public class RedPackageGrain : Orleans.Grain<RedPackageState>, IRedPackageGrain
{
    private readonly IObjectMapper _objectMapper;

    private const int RePackagePlaceMove = 2;

    public RedPackageGrain(IObjectMapper objectMapper)
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
        Guid senderId)
    {
        var result = new GrainResultDto<RedPackageDetailDto>();
        if (State.Status != RedPackageStatus.Init)
        {
            result.Success = false;
            result.Message = "RedPackage has been created";
            return result;
        }

        var bucketResult = GenerateBucket(input.Count, long.Parse(input.TotalAmount), minAmount,decimalIn,input.Type);
        State = _objectMapper.Map<SendRedPackageInputDto, RedPackageState>(input);
        State.Status = RedPackageStatus.NotClaimed;
        State.CreateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        State.EndTime = 0;
        State.ExpireTime = State.CreateTime + RedPackageConsts.ExpireTimeMs;
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

    public Task<GrainResultDto<RedPackageDetailDto>> GetRedPackage(int skip, int max, Guid userId)
    {
        var result = new GrainResultDto<RedPackageDetailDto>();
        result.Success = true;
        var dto = _objectMapper.Map<RedPackageState, RedPackageDetailDto>(State);
        dto.TotalCount = State.Items.Count;
        dto.IsCurrentUserGrabbed = State.Items.Any(item => item.UserId == userId);
        dto.Items = _objectMapper.Map<List<GrabItem>, List<GrabItemDto>>(State.Items.Skip(skip).Take(max).ToList());
        result.Data = dto;
        return Task.FromResult(result);
    }

    public async Task<GrainResultDto<bool>> ExpireRedPackage()
    {
        var result = new GrainResultDto<bool>();
        result.Success = true;
        result.Data = true;
        if (State.Status != RedPackageStatus.Cancelled)
        {
            State.Status = RedPackageStatus.Expired;
        }

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
                Amount = "",
                Status = State.Status
            };
            //repeated claim should return true 
            if (checkResult.Item2.Equals(RedPackageConsts.RedPackageUserGrabbed))
            {
                result.Success = true;
                result.Data = new GrabResultDto()
                {
                    Result = RedPackageGrabStatus.Success,
                    ErrorMessage = "",
                    Amount = State.Items.First(item => item.UserId.Equals(userId))?.Amount.ToString(),
                    Decimal = State.Decimal,
                    Status = State.Status,
                    ExpireTime = State.ExpireTime
                };
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
            ExpireTime = State.ExpireTime
        };

        await WriteStateAsync();
        return result;
    }

    public async Task<GrainResultDto<bool>> UpdateRedPackage(List<GrabItemDto> grabItemDtos)
    {
        var result = new GrainResultDto<bool>();
        result.Success = true;
        result.Data = true;
        if (State == null || State.Items.IsNullOrEmpty())
        {
            result.Success = false;
            result.Data = false;
            return result;
        }

        foreach (var grabItemDto in grabItemDtos.Where(o => !o.PaymentCompleted))
        {
            _ = State.Items
                .Where(item => item.UserId.Equals(grabItemDto.UserId) && grabItemDto.CaAddress.Equals(item.CaAddress))
                .First(item => item.PaymentCompleted = true);
        }

        await WriteStateAsync();
        return result;
    }

    public Task<GrainResultDto<RedPackageDetailDto>> GetRedPackage(Guid packageId)
    {
        var result = new GrainResultDto<RedPackageDetailDto>();
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

    private (bool, string) CheckRedPackagePermissions(Guid userId)
    {
        if (DateTimeOffset.Now.ToUnixTimeMilliseconds() > State.ExpireTime || State.Status == RedPackageStatus.Expired)
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

        if (State.Items.Any(item => item.UserId == userId))
        {
            return (false, RedPackageConsts.RedPackageUserGrabbed);
        }

        return (true, "");
    }

    private BucketItem GetBucket(Guid userId)
    {
        var random = new Random();
        var bucket = State.BucketNotClaimed[random.Next(State.BucketNotClaimed.Count)];
        bucket.UserId = userId;
        State.BucketNotClaimed.Remove(bucket);
        State.BucketClaimed.Add(bucket);
        return bucket;
    }

    private (List<BucketItem>, int) GenerateBucket(int count, long totalAmount, long minAmount, int decimalIn,
        RedPackageType type)
    {
        switch (type)
        {
            case RedPackageType.Random:
                return GenerateRandomBucket(count, totalAmount, minAmount,decimalIn);
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

      private (List<BucketItem>, int) GenerateRandomBucket(int count, long totalAmount, long minAmount,int decimalIn)
    {
        int luckyKingIndex = 0;
        long luckyKingAmount = minAmount;
        var bucket = new List<BucketItem>();
        var rest = totalAmount;
        Random random = new Random();
        int places = BucketRandomSpecialOperation(totalAmount, count,decimalIn, out bool specialOperation);
        for (var i = 0; i < count; i++)
        {
            bucket.Add(new BucketItem()
            {
                Amount = minAmount
            });
            rest -= minAmount;
        }

        for (var i = 0; i < count; i++)
        {
            if (rest <= minAmount)
            {
                break;
            }

            double randomNumber = random.NextDouble();
            long max = (rest / (count - i)) * 2;
            long money = Math.Max(minAmount, (long)(randomNumber * max));
            if (i == count - 1)
            {
                money = rest;
            } else if (specialOperation)
            {
                money= (money/(long)Math.Pow(10, decimalIn - places))*(long)Math.Pow(10, decimalIn - places);

            }
            bucket[i].Amount += money;
            if (bucket[i].Amount > luckyKingAmount)
            {
                luckyKingAmount = bucket[i].Amount;
                luckyKingIndex = i;
            }

            rest -= money;
        }
        if (bucket[count - 1].Amount > luckyKingAmount)
        {
            luckyKingAmount = bucket[count - 1].Amount;
            luckyKingIndex = count - 1;
        }

        bucket[luckyKingIndex].IsLuckyKing = true;

        return (bucket, luckyKingIndex);
    }
    private int BucketRandomSpecialOperation(long total, int count,int decimalIn,out bool specialOperation)
    {
        // little point in 0~2 
        int places = DecimalPlaces(total,decimalIn);
        if (places < 2)
        {
            long baseJudgeCount = (long)Math.Pow(10, decimalIn - 2 - places);
            if (baseJudgeCount * count > total)
            {
                specialOperation = false;
                return -1;
            }
            specialOperation = true;
            return places + 2;
        }
        else
        {
            long baseJudgeCount = (long)Math.Pow(10, decimalIn - places);
            if (baseJudgeCount * count > total)
            {
                specialOperation = false;
                return -1;
            }
            specialOperation = true;
            return places;
        }
    }

    private int DecimalPlaces(long count,int decimalIn)
    {
        long baseJudgeCount = (long)Math.Pow(10, decimalIn);
        decimal tempNum = (decimal)count / baseJudgeCount;
        int[] bits = decimal.GetBits(tempNum);
        int exponent = (bits[3] >> 16) & 0x1F;
        return exponent;
    }
}