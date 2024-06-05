using System;
using System.Threading.Tasks;
using CAServer.Monitor.Interceptor;
using CAServer.RedPackage;
using CAServer.RedPackage.Dtos;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("RedPackage")]
[Route("api/app/redpackage/")]
[IgnoreAntiforgeryToken]
public class RedPackageController : CAServerController
{
    private readonly IRedPackageAppService _redPackageAppService;

    public RedPackageController(IRedPackageAppService redPackageAppService)
    {
        _redPackageAppService = redPackageAppService;
    }

    [HttpPost("generate")]
    [Authorize]
    public async Task<GenerateRedPackageOutputDto> GenerateRedPackageAsync(GenerateRedPackageInputDto redPackageInput)
    {
        return await _redPackageAppService.GenerateRedPackageAsync(redPackageInput);
    }

    [HttpPost("send")]
    [Authorize]
    public async Task<SendRedPackageOutputDto> SendRedPackageAsync(SendRedPackageInputDto redPackageInput)
    {
        return await _redPackageAppService.SendRedPackageAsync(redPackageInput);
    }

    [HttpGet("getCreationResult")]
    [Authorize]
    public async Task<GetCreationResultOutputDto> GetCreationResultAsync(Guid sessionId)
    {
        return await _redPackageAppService.GetCreationResultAsync(sessionId);
    }

    [HttpGet("detail")]
    [Authorize]
    public async Task<RedPackageDetailDto> GetRedPackageDetailAsync(Guid id, int skipCount = 0, int maxResultCount = 0)
    {
        return await _redPackageAppService.GetRedPackageDetailAsync(id, skipCount, maxResultCount);
    }
    
    [HttpGet("config")]
    [AllowAnonymous]
    public async Task<RedPackageConfigOutput> GetRedPackageConfigAsync([CanBeNull] string chainId,
        [CanBeNull] string token)
    {
        return await _redPackageAppService.GetRedPackageConfigAsync(chainId,token);
    } 
    
    [HttpPost("grab")]
    [Authorize]
    public async Task<GrabRedPackageOutputDto> GrabRedPackageAsync(GrabRedPackageInputDto input)
    {
        return await _redPackageAppService.GrabRedPackageAsync(input);
    }

    [HttpGet("ip")]
    public string GetRemoteIp()
    {
        string ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
        return ipAddress;
    }
    
    [HttpGet("ip/async")]
    public async Task<string> GetRemoteIpAsync()
    {
        string ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
        return ipAddress;
    }
}