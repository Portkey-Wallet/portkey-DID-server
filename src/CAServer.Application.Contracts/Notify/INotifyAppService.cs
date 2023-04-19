using System;
using System.Threading.Tasks;
using CAServer.Notify.Dtos;

namespace CAServer.Notify;

public interface INotifyAppService
{
    Task<PullNotifyResultDto> PullNotifyAsync(PullNotifyDto input);
    Task<NotifyResultDto> CreateAsync(CreateNotifyDto notifyDto);
    Task<NotifyResultDto> UpdateAsync(Guid id, UpdateNotifyDto notifyDto);
    Task DeleteAsync(Guid id);
}