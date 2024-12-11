using CAServer.Grains.State.Notify;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Notify;

public class NotifyGrain : Grain<NotifyState>, INotifyGrain
{
    private readonly IObjectMapper _objectMapper;

    public NotifyGrain(IObjectMapper objectMapper)
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

    public Task<GrainResultDto<NotifyGrainDto>> GetNotifyAsync()
    {
        var result = new GrainResultDto<NotifyGrainDto>();
        if (State.IsDeleted || State.Id == Guid.Empty)
        {
            result.Message = NotifyMessage.NotExistMessage;
            return Task.FromResult(result);
        }

        result.Success = true;
        result.Data = _objectMapper.Map<NotifyState, NotifyGrainDto>(State);
        return Task.FromResult(result);
    }

    public async Task<GrainResultDto<NotifyGrainDto>> AddNotifyAsync(NotifyGrainDto notifyDto)
    {
        var result = new GrainResultDto<NotifyGrainDto>();
        if (!State.IsDeleted && State.Id != Guid.Empty)
        {
            result.Message = NotifyMessage.ExistMessage;
            return result;
        }

        State = _objectMapper.Map<NotifyGrainDto, NotifyState>(notifyDto);
        State.Id = this.GetPrimaryKey();

        State.RulesId = Guid.NewGuid();
        var rules = await AddOrUpdateRulesAsync(State.RulesId, notifyDto);

        await WriteStateAsync();

        var notifyData = _objectMapper.Map<NotifyState, NotifyGrainDto>(State);
        notifyData.AppVersions = rules.AppVersions;
        notifyData.DeviceBrands = rules.DeviceBrands;
        notifyData.DeviceTypes = rules.DeviceTypes;
        notifyData.OperatingSystemVersions = rules.OperatingSystemVersions;
        notifyData.SendTypes = rules.SendTypes;
        notifyData.IsApproved = rules.IsApproved;
        notifyData.Countries = rules.Countries;

        result.Success = true;
        result.Data = notifyData;
        return result;
    }

    public async Task<GrainResultDto<NotifyGrainDto>> UpdateNotifyAsync(NotifyGrainDto notifyDto)
    {
        var result = new GrainResultDto<NotifyGrainDto>();
        if (State.IsDeleted || State.Id == Guid.Empty)
        {
            result.Message = NotifyMessage.NotExistMessage;
            return result;
        }

        var rules = await AddOrUpdateRulesAsync(State.RulesId, notifyDto);
        State = _objectMapper.Map<NotifyGrainDto, NotifyState>(notifyDto);
        State.Id = this.GetPrimaryKey();
        State.RulesId = rules.Id;
        
        await WriteStateAsync();

        var notifyData = _objectMapper.Map<NotifyState, NotifyGrainDto>(State);
        notifyData.AppVersions = rules.AppVersions;
        notifyData.DeviceBrands = rules.DeviceBrands;
        notifyData.DeviceTypes = rules.DeviceTypes;
        notifyData.OperatingSystemVersions = rules.OperatingSystemVersions;
        notifyData.SendTypes = rules.SendTypes;
        notifyData.IsApproved = rules.IsApproved;
        notifyData.Countries = rules.Countries;

        result.Success = true;
        result.Data = notifyData;
        return result;
    }

    public async Task<GrainResultDto<NotifyGrainDto>> DeleteNotifyAsync(Guid notifyId)
    {
        var result = new GrainResultDto<NotifyGrainDto>();
        if (State.IsDeleted)
        {
            result.Message = NotifyMessage.NotExistMessage;
            return result;
        }

        State.IsDeleted = true;
        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<NotifyState, NotifyGrainDto>(State);
        return result;
    }

    private async Task<NotifyRulesGrainDto> AddOrUpdateRulesAsync(Guid id, NotifyGrainDto notifyDto)
    {
        var rulesGrain = GrainFactory.GetGrain<INotifyRulesGrain>(id);
        return await rulesGrain.AddOrUpdateNotifyAsync(
            _objectMapper.Map<NotifyGrainDto, NotifyRulesGrainDto>(notifyDto));
    }
}