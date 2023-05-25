using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Notify;
using CAServer.Notify.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Notify")]
[Route("api/app/notify")]
public class NotifyController
{
    private readonly INotifyAppService _notifyAppService;

    public NotifyController(INotifyAppService notifyAppService)
    {
        _notifyAppService = notifyAppService;
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<NotifyResultDto> CreateAsync(CreateNotifyDto notifyDto)
    {
        return await _notifyAppService.CreateAsync(notifyDto);
    }

    [HttpPost("createFromCms")]
    [Authorize(Roles = "admin")]
    public async Task<List<NotifyResultDto>> CreateFromCmsAsync([FromForm] string version)
    {
        return await _notifyAppService.CreateFromCmsAsync(version);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<NotifyResultDto> UpdateAsync(Guid id, UpdateNotifyDto notifyDto)
    {
        return await _notifyAppService.UpdateAsync(id, notifyDto);
    }

    [HttpPut("updateFromCms/{id}")]
    [Authorize(Roles = "admin")]
    public async Task<NotifyResultDto> UpdateFromCmsAsync(Guid id)
    {
        return await _notifyAppService.UpdateFromCmsAsync(id);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task DeleteAsync(Guid id)
    {
        await _notifyAppService.DeleteAsync(id);
    }

    [HttpPost("pullNotify")]
    public async Task<PullNotifyResultDto> PullNotifyAsync(PullNotifyDto input)
    {
        return await _notifyAppService.PullNotifyAsync(input);
    }
}