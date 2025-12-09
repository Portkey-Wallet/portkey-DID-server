namespace CAServer.Grains.Grain.Notify;

public interface INotifyGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<NotifyGrainDto>> GetNotifyAsync();
    Task<GrainResultDto<NotifyGrainDto>> AddNotifyAsync(NotifyGrainDto notifyDto);
    Task<GrainResultDto<NotifyGrainDto>> UpdateNotifyAsync(NotifyGrainDto notifyDto);
    Task<GrainResultDto<NotifyGrainDto>> DeleteNotifyAsync(Guid notifyId);
}