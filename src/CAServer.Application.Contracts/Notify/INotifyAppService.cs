using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Notify.Dtos;

namespace CAServer.Notify;

public interface INotifyAppService
{
    Task<int> FireAsync(string token, string title, string content);
    Task<PullNotifyResultDto> PullNotifyAsync(PullNotifyDto input);
    Task<NotifyResultDto> CreateAsync(CreateNotifyDto notifyDto);
    Task<NotifyResultDto> UpdateAsync(Guid id, UpdateNotifyDto notifyDto);
    Task DeleteAsync(Guid id);

    Task<List<NotifyResultDto>> CreateFromCmsAsync(string version);
    Task<NotifyResultDto> UpdateFromCmsAsync(Guid id);
}