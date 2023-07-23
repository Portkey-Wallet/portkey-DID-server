using CAServer.AppleMigrate;
using CAServer.AppleMigrate.Dtos;
using CAServer.AppleMigrate.Modle;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("AppleMigrate")]
[Route("api/app/apple")]
[IgnoreAntiforgeryToken]
public class AppleMigrateController : CAServerController
{
    private readonly IAppleMigrateAppService _appleMigrateAppService;

    public AppleMigrateController(IAppleMigrateAppService appleMigrateAppService)
    {
        _appleMigrateAppService = appleMigrateAppService;
    }

    [HttpPost("migrate")]
    public async Task<AppleMigrateResponseDto> MigrateAsync(AppleMigrateRequestDto input)
    {
        return await _appleMigrateAppService.MigrateAsync(input);
    }
    
    [HttpGet("getSub")]
    public async Task<string> GetSubAsync()
    {
        throw new NotImplementedException();
    }

    [HttpGet("getNewUserId")]
    public async Task<string> GetNewUserIdAsync(GetNewUserIdRequestDto input)
    {
        throw new NotImplementedException();
    }

    [HttpPost("migrateAll")]
    public async Task<int> MigrateAllAsync()
    {
        throw new NotImplementedException();
    }
}