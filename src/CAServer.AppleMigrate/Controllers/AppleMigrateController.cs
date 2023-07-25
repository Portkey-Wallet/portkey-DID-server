using CAServer.AppleMigrate;
using CAServer.AppleMigrate.Dtos;
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
    private readonly IAppleMigrateProvider _appleMigrateProvider;

    public AppleMigrateController(IAppleMigrateAppService appleMigrateAppService,
        IAppleMigrateProvider appleMigrateProvider)
    {
        _appleMigrateAppService = appleMigrateAppService;
        _appleMigrateProvider = appleMigrateProvider;
    }

    [HttpPost("migrate")]
    public async Task<AppleMigrateResponseDto> MigrateAsync(AppleMigrateRequestDto input)
    {
        return await _appleMigrateAppService.MigrateAsync(input);
    }

    [HttpGet("getSub")]
    public async Task<GetSubDto> GetSubAsync(string userId)
    {
        return await _appleMigrateProvider.GetSubAsync(userId);
    }

    [HttpGet("getNewUserId")]
    public async Task<GetNewUserIdDto> GetNewUserIdAsync(string userId)
    {
        return await _appleMigrateProvider.GetNewUserIdAsync(userId);
    }

    [HttpPost("migrateAll")]
    public async Task<int> MigrateAllAsync()
    {
        throw new NotImplementedException();
    }

    [HttpGet("getClientSecret")]
    public string GetSecret()
    {
        return _appleMigrateProvider.GetSecret();
    }

    [HttpGet("getAccessToken")]
    public async Task<string> GetAccessToken(string clientId, string clientSecret)
    {
        return await _appleMigrateProvider.GetAccessToken(clientId, clientSecret);
    }
}