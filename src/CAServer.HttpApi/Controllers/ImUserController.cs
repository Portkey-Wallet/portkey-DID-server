using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Contacts;
using CAServer.ImUser;
using CAServer.ImUser.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ImUser")]
[Route("api/app/imUsers")]
[IgnoreAntiforgeryToken]
[Authorize]
public class ImUserController : CAServerController
{
    private readonly IContactAppService _contactAppService;
    private readonly IImUserAppService _imUserAppService;

    public ImUserController(IContactAppService contactAppService, IImUserAppService imUserAppService)
    {
        _contactAppService = contactAppService;
        _imUserAppService = imUserAppService;
    }

    [HttpPost("names")]
    public async Task<List<GetNamesResultDto>> GetNameAsync(List<Guid> input)
    {
        return await _contactAppService.GetNameAsync(input);
    }

    [HttpGet("holder")]
    public async Task<HolderInfoResultDto> GetHolderInfoAsync(Guid userId)
    {
        return await _imUserAppService.GetHolderInfoAsync(userId);
    }
    
    [HttpGet("holder/list")]
    public async Task<List<Guid>> ListHolderInfoAsync(string keyword)
    {
        return await _imUserAppService.ListHolderInfoAsync(keyword);
    }
    
    [HttpGet("holders")]
    public async Task<List<HolderInfoResultDto>> GetHolderInfosAsync(List<Guid> userIds)
    {
        return await _imUserAppService.GetHolderInfosAsync(userIds);
    }
}