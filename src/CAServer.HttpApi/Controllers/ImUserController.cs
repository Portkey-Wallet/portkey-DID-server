using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Contacts;
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

    public ImUserController(IContactAppService contactAppService)
    {
        _contactAppService = contactAppService;
    }
    
    [HttpPost("names")]
    public async Task<List<GetNamesResultDto>> GetNameAsync(List<Guid> input)
    {
        return await _contactAppService.GetNameAsync(input);
    }
}