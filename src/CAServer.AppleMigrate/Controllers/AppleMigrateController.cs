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
    private readonly IAppleGuardianProvider _appleGuardianProvider;

    public AppleMigrateController(IAppleMigrateAppService appleMigrateAppService,
        IAppleMigrateProvider appleMigrateProvider,
        IAppleGuardianProvider appleGuardianProvider)
    {
        _appleMigrateAppService = appleMigrateAppService;
        _appleMigrateProvider = appleMigrateProvider;
        _appleGuardianProvider = appleGuardianProvider;
    }

    [HttpPost("migrate")]
    public async Task<AppleMigrateResponseDto> MigrateAsync(AppleMigrateRequestDto input)
    {
        return await _appleMigrateAppService.MigrateAsync(input);
    }
    
    [HttpPost("migrateAll")]
    public async Task<int> MigrateAllAsync(bool retry)
    {
        return await _appleMigrateAppService.MigrateAllAsync(retry);
    }

    [HttpGet("getSub")]
    public async Task<GetSubDto> GetSubAsync(string userId)
    {
        return await _appleMigrateProvider.GetSubAsync(userId);
    }
    
    [HttpGet("getTransferInfoFromCache")]
    public async Task<AppleUserTransferInfo> GetTransferInfoFromCache(string userId)
    {
        return await _appleMigrateProvider.GetTransferInfoFromCache(userId);
    }

    [HttpGet("getNewUserId")]
    public async Task<GetNewUserIdDto> GetNewUserIdAsync(string userId)
    {
        return await _appleMigrateProvider.GetNewUserIdAsync(userId);
    }
    
    [HttpGet("getSecretAndAccessToken")]
    public async Task<Dictionary<string, string>> GetSecretAndAccessToken()
    {
        return await _appleMigrateProvider.GetSecretAndAccessToken();
    }

    [HttpGet("getAccessToken")]
    public async Task<string> GetAccessToken(string clientId, string clientSecret)
    {
        return await _appleMigrateProvider.GetAccessToken(clientId, clientSecret);
    }

    [HttpPost("setAppleGuardianIntoCache")]
    public async Task<int> SetAppleGuardianIntoCache()
    {
        return await _appleGuardianProvider.SetAppleGuardianIntoCache();
    }

    [HttpGet("getAppleGuardianIntoCache")]
    public async Task<AppleUserTransfer> GetAppleGuardianIntoCache()
    {
        return await _appleGuardianProvider.GetAppleGuardianIntoCache();
    }

    [HttpPost("setTransferSub")]
    public async Task<int> SetTransferSubAsync()
    {
        return await _appleMigrateProvider.SetTransferSubAsync();
    }

    [HttpPost("setNewUserInfo")]
    public async Task<int> SetNewUserInfoAsync()
    {
        return await _appleMigrateProvider.SetNewUserInfoAsync();
    }
}