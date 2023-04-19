using CAServer.Grains.State.Notify;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Notify;

public class NotifyRulesGrain : Grain<NotifyRulesState>, INotifyRulesGrain
{
    private readonly IObjectMapper _objectMapper;

    public NotifyRulesGrain(IObjectMapper objectMapper)
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

    public async Task<NotifyRulesGrainDto> AddOrUpdateNotifyAsync(NotifyRulesGrainDto rulesGrainDto)
    {
        State = _objectMapper.Map<NotifyRulesGrainDto, NotifyRulesState>(rulesGrainDto);
        State.Id = this.GetPrimaryKey();
        
        await WriteStateAsync();
        return _objectMapper.Map<NotifyRulesState, NotifyRulesGrainDto>(State);
    }
}