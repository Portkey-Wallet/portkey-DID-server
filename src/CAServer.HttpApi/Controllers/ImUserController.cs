using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Contacts;
using CAServer.ImUser;
using CAServer.ImUser.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
    private readonly ILogger<ImUserController> _logger;

    public ImUserController(IContactAppService contactAppService, IImUserAppService imUserAppService,
        ILogger<ImUserController> logger)
    {
        _contactAppService = contactAppService;
        _imUserAppService = imUserAppService;
        _logger = logger;
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
    public async Task<List<HolderInfoResultDto>> GetUserInfoAsync(List<Guid> userIds)
    {
        return await _imUserAppService.GetUserInfoAsync(userIds);
    }
}