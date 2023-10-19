using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Commons.Dtos;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ThirdPart")]
[Route("api/app/thirdPart/")]
[Authorize]
public class ThirdPartUserController : CAServerController
{
    private readonly IAlchemyServiceAppService _alchemyServiceAppService;
    private readonly IThirdPartOrderAppService _thirdPartOrdersAppService;
    private readonly IRampService _rampService;

    public ThirdPartUserController(IThirdPartOrderAppService thirdPartOrderAppService,
        IAlchemyServiceAppService alchemyServiceAppService,
        IRampService rampService)
    {
        _thirdPartOrdersAppService = thirdPartOrderAppService;
        _alchemyServiceAppService = alchemyServiceAppService;
        _rampService = rampService;
    }


    [HttpPost("order")]
    public async Task<OrderCreatedDto> CreateThirdPartOrderAsync(
        CreateUserOrderDto input)
    {
        return await _thirdPartOrdersAppService.CreateThirdPartOrderAsync(input);
    }

    [HttpGet("orders")]
    public async Task<PageResultDto<OrderDto>> GetThirdPartOrdersAsync(GetUserOrdersDto input)
    {
        return await _thirdPartOrdersAppService.GetThirdPartOrdersAsync(input);
    }

    [HttpGet("ramp/coverage")]
    public async Task<CommonResponseDto<RampCoverage>> GetRampCoverage(string type)
    {
        return await _rampService.GetRampCoverageAsync(type);
    }

    [HttpGet("ramp/detail")]
    public async Task<CommonResponseDto<RampDetail>> GetRampDetail(RampDetailRequest request)
    {
        return await _rampService.GetRampDetailAsync(request);
    }

    [HttpGet("ramp/detail/providers")]
    public async Task<CommonResponseDto<RampProviderDetail>> GetRampProvidersDetail(RampDetailRequest request)
    {
        return await _rampService.GetRampProvidersDetailAsync(request);
    }

    [HttpPost("alchemy/token")]
    public async Task<AlchemyBaseResponseDto<AlchemyTokenDataDto>> GetAlchemyFreeLoginTokenAsync(
        GetAlchemyFreeLoginTokenDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyFreeLoginTokenAsync(input);
    }

    [HttpPost("alchemy/token/nft")]
    public async Task<AlchemyBaseResponseDto<AlchemyTokenDataDto>> GetAlchemyNftFreeLoginTokenAsync(
        GetAlchemyFreeLoginTokenDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyNftFreeLoginTokenAsync(input);
    }

    [HttpGet("alchemy/signature")]
    public async Task<AlchemySignatureResultDto> GetAlchemySignatureAsync(GetAlchemySignatureDto input)
    {
        return await _alchemyServiceAppService.GetAlchemySignatureAsync(input);
    }

    [HttpGet("alchemy/signature/api")]
    public async Task<AlchemyBaseResponseDto<string>> GetAlchemyApiSignatureAsync(Dictionary<string, string> input)
    {
        return await _alchemyServiceAppService.GetAlchemyApiSignatureAsync(input);
    }
}