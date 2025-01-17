using CAServer.Grains.Grain;
using CAServer.Grains.Grain.ZeroHoldings;
using CAServer.Grains.State.UserExtraInfo;
using Volo.Abp.ObjectMapping;

public class ZeroHoldingsConfigGrain: Grain<ZeroHoldingsConfigState>, IZeroHoldingsConfigGrain
{
    private readonly IObjectMapper _objectMapper;

    public ZeroHoldingsConfigGrain(IObjectMapper objectMapper)
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

    public async Task<bool> AddOrUpdateAsync(ZeroHoldingsGrainDto config)
    {
        State = _objectMapper.Map<ZeroHoldingsGrainDto, ZeroHoldingsConfigState>(config);
        State.Id = this.GetPrimaryKeyString();

        await WriteStateAsync();
        return true;
    }

    public Task<GrainResultDto<ZeroHoldingsGrainDto>> GetAsync(Guid userId)
    {
        if (State.UserId == Guid.Empty)
        {
            State.UserId = userId;
        }

        return Task.FromResult(new GrainResultDto<ZeroHoldingsGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<ZeroHoldingsConfigState, ZeroHoldingsGrainDto>(State)
        });
    }
}