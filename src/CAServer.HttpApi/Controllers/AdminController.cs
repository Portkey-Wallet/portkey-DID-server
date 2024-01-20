using System;
using System.Threading.Tasks;
using AElf;
using CAServer.Admin.Dtos;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Admin")]
[Route("api/app/admin/")]
public class AdminController : CAServerController
{
    
    private readonly IOptionsMonitor<AuthServerOptions> _authServerOptions;
    private readonly IThirdPartOrderAppService _thirdPartOrderAppService;

    public AdminController(IOptionsMonitor<AuthServerOptions> authServerOptions, IThirdPartOrderAppService thirdPartOrderAppService)
    {
        _authServerOptions = authServerOptions;
        _thirdPartOrderAppService = thirdPartOrderAppService;
    }


    [HttpGet("config")]
    public Task<CommonResponseDto<AdminConfigResponse>> Config()
    {
        return Task.FromResult(new CommonResponseDto<AdminConfigResponse>(new AdminConfigResponse
        {
            AuthServer = _authServerOptions.CurrentValue.Authority,
            ClientId = _authServerOptions.CurrentValue.SwaggerClientId
        }));
    }

    [HttpGet("mfa")]
    public Task<CommonResponseDto<MfaResponse>> GetNewMfaCode()
    {
        var userName = "";
        //TODO var userName = 
        var code = _thirdPartOrderAppService.GenerateGoogleAuthCode(RsaHelper.GenerateRsaKeyPair().Private.ToBson().ToHex(), userName, "portkey-admin");
        return Task.FromResult(new CommonResponseDto<MfaResponse>(new MfaResponse
        {
            KeyId = "", //TODO cache
            CodeImage = code.QrCodeSetupImageUrl,
            ManualEntryKey = code.ManualEntryKey,
        }));
    }
    

    [HttpPost("mfa")]
    public Task<CommonResponseDto<MfaResponse>> SetNewMfaCode()
    {
        //TODO 
        throw new NotImplementedException();
    }
    
    
    [Authorize]
    [HttpGet("user")]
    public Task<CommonResponseDto<AdminConfigResponse>> GetUser()
    {
        //TODO
        return Task.FromResult(new CommonResponseDto<AdminConfigResponse>(new AdminConfigResponse
        {
            AuthServer = _authServerOptions.CurrentValue.Authority,
            ClientId = _authServerOptions.CurrentValue.SwaggerClientId
        }));
    }
    

    [HttpGet("ramp/orders")]
    public async Task<CommonResponseDto<PagedResultDto<OrderDto>>> GetOrder(GetThirdPartOrderConditionDto request)
    {
        var pager = await _thirdPartOrderAppService.GetThirdPartOrdersByPageAsync(request);
        return new CommonResponseDto<PagedResultDto<OrderDto>>(pager);
    }

    [HttpPost("ramp/order")]
    public async Task<CommonResponseDto<Empty>> UpdateOrder(OrderDto orderDto)
    {
        //TODO Google MFA
        return await _thirdPartOrderAppService.UpdateRampOrder(orderDto);
    }
    
}