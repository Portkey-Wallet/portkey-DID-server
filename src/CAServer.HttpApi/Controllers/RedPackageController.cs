using System;
using System.Linq;
using System.Threading.Tasks;
using CAServer.EnumType;
using CAServer.RedPackage;
using CAServer.RedPackage.Dtos;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
    private readonly ILogger<RedPackageController> _logger;

    public RedPackageController(IRedPackageAppService redPackageAppService,
        ILogger<RedPackageController> logger)
    {
        _redPackageAppService = redPackageAppService;
        _logger = logger;
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
        if (!Enum.IsDefined(redPackageInput.RedPackageDisplayType))
        {
            redPackageInput.RedPackageDisplayType = RedPackageDisplayType.Common;
        }
        _logger.LogInformation("Controller SendRedPackageAsync:{0}", JsonConvert.SerializeObject(redPackageInput));
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
    public async Task<RedPackageDetailDto> GetRedPackageDetailAsync(Guid id, RedPackageDisplayType displayType, int skipCount = 0, int maxResultCount = 0)
    {
        if (!Enum.IsDefined(displayType))
        {
            displayType = RedPackageDisplayType.Common;
        }
        return await _redPackageAppService.GetRedPackageDetailAsync(id, displayType,skipCount, maxResultCount);
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
        var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress;
        _logger.LogInformation("IsIPv6Multicast:{0},IsIPv6Teredo:{1},IsIPv6LinkLocal:{2}," +
                               "IsIPv6SiteLocal:{3},IsIPv6UniqueLocal:{4},IsIPv4MappedToIPv6:{5}",
            remoteIpAddress.IsIPv6Multicast,
            remoteIpAddress.IsIPv6Teredo,
            remoteIpAddress.IsIPv6LinkLocal,
            remoteIpAddress.IsIPv6SiteLocal,
            remoteIpAddress.IsIPv6UniqueLocal,
            remoteIpAddress.IsIPv4MappedToIPv6);
        _logger.LogInformation("MapToIPv4:{0}", remoteIpAddress.MapToIPv4().ToString());
        _logger.LogInformation("MapToIPv6:{0}", remoteIpAddress.MapToIPv6().ToString());
        string ipAddress = remoteIpAddress?.ToString();
        return ipAddress;
    }
    
    [HttpGet("ip/async")]
    public async Task<string> GetRemoteIpAsync()
    {
        var clientIp = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(clientIp))
        {
            return clientIp;
        }

        var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress;
        if (remoteIpAddress == null)
        {
            return string.Empty;
        }
        return remoteIpAddress.IsIPv4MappedToIPv6 ? remoteIpAddress.MapToIPv4().ToString() : remoteIpAddress.MapToIPv6().ToString();
    }
}