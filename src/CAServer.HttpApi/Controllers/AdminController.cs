using System;
using System.Threading.Tasks;
using CAServer.Admin;
using CAServer.Admin.Dtos;
using CAServer.Commons;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Users;

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
    private readonly ITreasuryOrderProvider _treasuryOrderProvider;

    public AdminController(IOptionsMonitor<AuthServerOptions> authServerOptions,
        IThirdPartOrderAppService thirdPartOrderAppService, IAdminAppService adminAppService,
        ITreasuryOrderProvider treasuryOrderProvider)
    {
        _authServerOptions = authServerOptions;
        _thirdPartOrderAppService = thirdPartOrderAppService;
        _adminAppService = adminAppService;
        _treasuryOrderProvider = treasuryOrderProvider;
    }

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

    [Authorize]
    [HttpGet("user")]
    public async Task<CommonResponseDto<AdminUserResponse>> QueryUser()
    {
        var user = await _adminAppService.GetCurrentUserAsync();
        return new CommonResponseDto<AdminUserResponse>(user);
    }

    [Authorize]
    [HttpGet("mfa")]
    public Task<CommonResponseDto<MfaResponse>> GenerateNewMfaCode()
    {
        return Task.FromResult(new CommonResponseDto<MfaResponse>(_adminAppService.GenerateRandomMfa()));
    }

    [Authorize]
    [HttpPost("mfa")]
    public async Task<CommonResponseDto<Empty>> SetNewMfaCode(MfaRequest mfaRequest)
    {
        await _adminAppService.SetMfa(mfaRequest);
        return new CommonResponseDto<Empty>();
    }
    
    [Authorize]
    [HttpDelete("mfa")]
    public async Task<CommonResponseDto<Empty>> DeleteMfaCode(MfaRequest<Empty> mfaRequest)
    {
        await _adminAppService.AssertMfa(mfaRequest.GoogleTfaPin);
        await _adminAppService.ClearMfa(CurrentUser.GetId());
        return new CommonResponseDto<Empty>();
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("user/mfa")]
    public async Task<CommonResponseDto<Empty>> DeleteUserMfa(MfaRequest<Guid> mfaRequest)
    {
        await _adminAppService.ClearMfa(mfaRequest.Data);
        return new CommonResponseDto<Empty>();
    }

    [Authorize(Roles = "admin")]
    [HttpGet("ramp/orders")]
    public async Task<CommonResponseDto<PagedResultDto<OrderDto>>> GetOrder(GetThirdPartOrderConditionDto request)
    {
        var pager = await _thirdPartOrderAppService.GetThirdPartOrdersByPageAsync(request, OrderSectionEnum.NftSection,
            OrderSectionEnum.SettlementSection, OrderSectionEnum.OrderStateSection);
        return new CommonResponseDto<PagedResultDto<OrderDto>>(pager);
    }

    [Authorize(Roles = "admin")]
    [HttpPost("ramp/order")]
    public async Task<CommonResponseDto<Empty>> UpdateOrder(MfaRequest<OrderDto> updateOrderRequest)
    {
        await _adminAppService.AssertMfa(updateOrderRequest.GoogleTfaPin);
        return await _thirdPartOrderAppService.UpdateRampOrder(updateOrderRequest.Data);
    }

    [Authorize(Roles = "admin")]
    [HttpGet("treasury/orders")]
    public async Task<CommonResponseDto<PagedResultDto<TreasuryOrderDto>>> GetTreasuryOrder(
        TreasuryOrderCondition request)
    {
        var pager = await _treasuryOrderProvider.QueryOrderAsync(request);
        return new CommonResponseDto<PagedResultDto<TreasuryOrderDto>>(pager);
    }

    [Authorize(Roles = "admin")]
    [HttpPost("treasury/order")]
    public async Task<CommonResponseDto<Empty>> UpdateTreasuryOrder(MfaRequest<TreasuryOrderDto> updateOrderRequest)
    {
        await _adminAppService.AssertMfa(updateOrderRequest.GoogleTfaPin);
        await _treasuryOrderProvider.DoSaveOrder(updateOrderRequest.Data);
        return new CommonResponseDto<Empty>();
    }
}