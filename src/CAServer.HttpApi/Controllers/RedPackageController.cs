using System;
using System.Threading.Tasks;
using CAServer.RedPackage;
using CAServer.RedPackage.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("RedPackage")]
[Route("api/app/redpackage/")]
[Authorize]
[IgnoreAntiforgeryToken]
public class RedPackageController : CAServerController
{
    private readonly IRedPackageAppService _redPackageAppService;

    public RedPackageController(IRedPackageAppService redPackageAppService)
    {
        _redPackageAppService = redPackageAppService;
    }

    [HttpPost("generate")]
    public async Task<GenerateRedPackageOutputDto> GenerateRedPackageAsync(GenerateRedPackageInputDto redPackageInput)
    {
        return await _redPackageAppService.GenerateRedPackageAsync(redPackageInput);
    }

    [HttpPost("send")]
    public async Task<SendRedPackageOutputDto> SendRedPackageAsync(SendRedPackageInputDto redPackageInput)
    {
        return await _redPackageAppService.SendRedPackageAsync(redPackageInput);
    }

    [HttpGet("getcreationresult")]
    public async Task<GetCreationResultOutputDto> GetCreationResultAsync(Guid sessionId)
    {
        return await _redPackageAppService.GetCreationResultAsync(sessionId);
    }

    [HttpGet("detail")]
    public async Task<RedPackageDetailDto> GetRedPackageDetailAsync(Guid id, int skipCount, int maxResultCount)
    {
        return await _redPackageAppService.GetRedPackageDetailAsync(id, skipCount, maxResultCount);
    }
}