using CAServer.Commons;
using CAServer.Grains.State.Order;
using CAServer.ThirdPart;
using Microsoft.Extensions.Logging;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.ThirdPart;

public interface ITreasuryOrderGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<TreasuryOrderDto>> SaveOrUpdateAsync(TreasuryOrderDto orderDto);

    Task<GrainResultDto<TreasuryOrderDto>> GetAsync();
}

public class TreasuryOrderGrain : Grain<TreasuryOrderState>, ITreasuryOrderGrain
{
    private readonly ILogger<TreasuryOrderGrain> _logger;
    private readonly IObjectMapper _objectMapper;

    public TreasuryOrderGrain(IObjectMapper objectMapper, ILogger<TreasuryOrderGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
    }


    public async Task<GrainResultDto<TreasuryOrderDto>> SaveOrUpdateAsync(TreasuryOrderDto orderDto)
    {
        var createTime = State.CreateTime;
        if (orderDto.Version < State.Version)
        {
            _logger.LogWarning(
                "TreasuryOrderGrain SaveOrUpdateAsync version err, current:{OrderId}-{Version}-{Status}, input:{InOrderId}-{InVersion}-{InStatus}",
                State.Id, State.Version, State.Status, orderDto.Id, orderDto.Version, orderDto.Status);
            return new GrainResultDto<TreasuryOrderDto>().Error("Data expired, current version is " + State.Version);
        }

        _objectMapper.Map(orderDto, State);
        State.Id = this.GetPrimaryKey();
        State.CreateTime = createTime == 0 ? DateTime.UtcNow.ToUtcMilliSeconds() : createTime;
        State.LastModifyTime = DateTime.UtcNow.ToUtcMilliSeconds();
        State.Version++;

        await WriteStateAsync();

        return new GrainResultDto<TreasuryOrderDto>(_objectMapper.Map<TreasuryOrderState, TreasuryOrderDto>(State));
    }


    public Task<GrainResultDto<TreasuryOrderDto>> GetAsync()
    {
        return Task.FromResult(
            new GrainResultDto<TreasuryOrderDto>(_objectMapper.Map<TreasuryOrderState, TreasuryOrderDto>(State)));
    }
}