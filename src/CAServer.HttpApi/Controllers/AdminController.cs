using System;
using System.Threading.Tasks;
using AElf;
using CAServer.Admin;
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
    private readonly IAdminAppService _adminAppService;

    public AdminController(IOptionsMonitor<AuthServerOptions> authServerOptions,
        IThirdPartOrderAppService thirdPartOrderAppService, IAdminAppService adminAppService)
    {
        _authServerOptions = authServerOptions;
        _thirdPartOrderAppService = thirdPartOrderAppService;
        _adminAppService = adminAppService;
    }

    // [Authorize]
    [HttpGet]
    [Route("config")]
    public Task<CommonResponseDto<AdminConfigResponse>> Config()
    {
        return Task.FromResult(new CommonResponseDto<AdminConfigResponse>(new AdminConfigResponse
        {
            AuthServer = _authServerOptions.CurrentValue.Authority,
            ClientId = _authServerOptions.CurrentValue.SwaggerClientId
        }));
    }

    // [Authorize]
    [HttpGet]
    [Route("mfa")]
    public Task<CommonResponseDto<MfaResponse>> GenerateNewMfaCode()
    {
        return Task.FromResult(new CommonResponseDto<MfaResponse>(_adminAppService.GenerateRandomMfa()));
    }
    
    // [Authorize]
    [HttpPost]
    [Route("mfa")]
    public async Task<CommonResponseDto<Empty>> SetNewMfaCode(MfaRequest mfaRequest)
    {
        await _adminAppService.SetMfa(mfaRequest);
        return new CommonResponseDto<Empty>();
    }

    // [Authorize]
    [HttpGet]
    [Route("user")]
    public async Task<CommonResponseDto<AdminUserResponse>> QueryUser()
    {
        var user = await _adminAppService.GetCurrentUserAsync();
        return new CommonResponseDto<AdminUserResponse>(user);
    }
    
    // [Authorize]
    [HttpGet]
    [Route("ramp/orders")]
    public async Task<CommonResponseDto<PagedResultDto<OrderDto>>> GetOrder(GetThirdPartOrderConditionDto request)
    {
        var pager = await _thirdPartOrderAppService.GetThirdPartOrdersByPageAsync(request);
        return new CommonResponseDto<PagedResultDto<OrderDto>>(pager);
    }

    // [Authorize]
    [HttpPost]
    [Route("ramp/order")]
    public async Task<CommonResponseDto<Empty>> UpdateOrder(UpdateOrderRequest updateOrderRequest)
    {
        await _adminAppService.AssertMfa(updateOrderRequest.TfaPin);
        return await _thirdPartOrderAppService.UpdateRampOrder(updateOrderRequest.OrderDto);
    }
}