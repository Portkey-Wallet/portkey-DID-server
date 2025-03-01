using System;
using System.Threading.Tasks;
using Asp.Versioning;
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
    public async Task<RedPackageDetailDto> GetRedPackageDetailAsync(Guid id, RedPackageDisplayType redPackageDisplayType, int skipCount = 0, int maxResultCount = 0)
    {
        _logger.LogInformation("Controller redPackageId:{0} displayType:{1} Enum.IsDefined(displayType):{2}", id, redPackageDisplayType, Enum.IsDefined(redPackageDisplayType));
        if (!Enum.IsDefined(redPackageDisplayType))
        {
            redPackageDisplayType = RedPackageDisplayType.Common;
        }
        return await _redPackageAppService.GetRedPackageDetailAsync(id, redPackageDisplayType, skipCount, maxResultCount);
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
        if (!Enum.IsDefined(input.RedPackageDisplayType))
        {
            input.RedPackageDisplayType = RedPackageDisplayType.Common;
        }
        return await _redPackageAppService.GrabRedPackageAsync(input);
    }
    
    [HttpPost("logged/grab")]
    public async Task<GrabRedPackageOutputDto> LoggedGrabRedPackageAsync(GrabRedPackageInputDto input)
    {
        return await _redPackageAppService.LoggedGrabRedPackageAsync(input);
    }
}