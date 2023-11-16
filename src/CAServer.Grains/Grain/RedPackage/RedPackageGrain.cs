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
        decimal minAmount,
        Guid senderId)
    {
        var result = new GrainResultDto<RedPackageDetailDto>();
        if (State.Status != RedPackageStatus.Init)
        {
            result.Success = false;
            result.Message = "RedPackage has been created";
            return result;
        }

        var bucketResult = GenerateBucket(input.Count, input.TotalAmount, minAmount, input.Type);
        State = _objectMapper.Map<SendRedPackageInputDto, RedPackageState>(input);
        State.Status = RedPackageStatus.NotClaimed;
        State.CreateTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        State.EndTime = 0;
        State.ExpireTime = State.CreateTime + RedPackageConsts.ExpireTime;
        State.Decimal = decimalIn;
        State.Bucket = bucketResult.Item1;
        State.LuckyKingIndex = bucketResult.Item2;
        State.SenderId = senderId;
        
        await WriteStateAsync();
        
        result.Success = true;
        result.Data = _objectMapper.Map<RedPackageState, RedPackageDetailDto>(State);
        return result;
    }

    public Task<GrainResultDto<RedPackageDetailDto>> GetRedPackage(int skip, int max)
    {
        var result = new GrainResultDto<RedPackageDetailDto>();
        result.Success = true;
        var dto = _objectMapper.Map<RedPackageState, RedPackageDetailDto>(State);
        dto.Items = _objectMapper.Map<List<GrabItem>, List<GrabItemDto>>(State.Items.Skip(skip).Take(max).ToList());
        result.Data = dto;
        return Task.FromResult(result);
    }

    public Task<GrainResultDto<bool>> IsUserIdGrab(Guid userId)
    {
        var result = new GrainResultDto<bool>();
        result.Success = true;
        result.Data = State.Items.Any(item => item.UserId == userId);
        return Task.FromResult(result);
    }

    public async Task<GrainResultDto<bool>> DeleteRedPackage()
    {
        var result = new GrainResultDto<bool>();
        result.Success = true;
        result.Data = true;
        State.Status = RedPackageStatus.Expired;
        await WriteStateAsync();
        return result;
    }

    private (List<decimal>, int) GenerateBucket(int count, decimal totalAmount, decimal minAmount, RedPackageType type)
    {
        switch (type)
        {
            case RedPackageType.Random:
                return GenerateRandomBucket(count, totalAmount, minAmount);
            case RedPackageType.Fixed:
                return GenerateFixBucket(count, totalAmount);
            case RedPackageType.QuickTransfer:
                return GenerateFixBucket(count, totalAmount);
        }

        return (new List<decimal>(), 0);
    }

    private (List<decimal>, int) GenerateFixBucket(int count, decimal totalAmount)
    {
        var avg = totalAmount / count;
        var bucket = new List<decimal>();
        var rest = totalAmount;
        for (var i = 0; i < count; i++)
        {
            bucket.Add(avg);
            rest -= avg;
        }

        bucket[count - 1] += rest;
        return (bucket, 0);
    }

    private (List<decimal>, int) GenerateRandomBucket(int count, decimal totalAmount, decimal minAmount)
    {
        Random random = new Random();
        int luckyKingIndex = 0;
        decimal luckyKingAmount = minAmount;
        var bucket = new List<decimal>();
        var rest = totalAmount;
        for (var i = 0; i < count; i++)
        {
            bucket.Add(minAmount);
            rest -= minAmount;
        }

        for (var i = 0; i < count; i++)
        {
            if (rest <= minAmount)
            {
                break;
            }

            double randomNumber = random.NextDouble();
            decimal max = (rest / (count - i)) * 2;
            decimal money = Math.Max(minAmount, (decimal)randomNumber * max);
            bucket[i] += money;
            if (bucket[i] > luckyKingAmount)
            {
                luckyKingAmount = bucket[i];
                luckyKingIndex = i;
            }

            rest -= money;
        }

        bucket[count - 1] += rest;
        if (bucket[count - 1] > luckyKingAmount)
        {
            luckyKingAmount = bucket[count - 1];
            luckyKingIndex = count - 1;
        }

        return (bucket, luckyKingIndex);
    }
}