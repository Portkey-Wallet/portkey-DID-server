namespace CAServer.Grains.Grain.Notify;

public interface INotifyRulesGrain : IGrainWithGuidKey
{
    Task<NotifyRulesGrainDto> AddOrUpdateNotifyAsync(NotifyRulesGrainDto rulesGrainDto);
}