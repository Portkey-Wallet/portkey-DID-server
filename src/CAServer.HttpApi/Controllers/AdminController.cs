using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.Admin;
using CAServer.Admin.Dtos;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Options;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<AdminController> _logger;
    private readonly IOptionsMonitor<AuthServerOptions> _authServerOptions;
    private readonly IThirdPartOrderAppService _thirdPartOrderAppService;
    private readonly IAdminAppService _adminAppService;
    private readonly ITreasuryOrderProvider _treasuryOrderProvider;

    public AdminController(IOptionsMonitor<AuthServerOptions> authServerOptions,
        IThirdPartOrderAppService thirdPartOrderAppService, IAdminAppService adminAppService,
        ITreasuryOrderProvider treasuryOrderProvider, ILogger<AdminController> logger)
    {
        _authServerOptions = authServerOptions;
        _thirdPartOrderAppService = thirdPartOrderAppService;
        _adminAppService = adminAppService;
        _treasuryOrderProvider = treasuryOrderProvider;
        _logger = logger;
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
        await _adminAppService.SetMfaAsync(mfaRequest);
        return new CommonResponseDto<Empty>();
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("user/mfa")]
    public async Task<CommonResponseDto<Empty>> DeleteUserMfa(MfaRequest<Guid> mfaRequest)
    {
        await _adminAppService.ClearMfaAsync(mfaRequest.Data);
        return new CommonResponseDto<Empty>();
    }

    [Authorize(Roles = "OrderManager,OrderViewer")]
    [HttpGet("ramp/orders")]
    public async Task<CommonResponseDto<PagedResultDto<OrderDto>>> GetOrder(GetThirdPartOrderConditionDto request)
    {
        var pager = await _thirdPartOrderAppService.GetThirdPartOrdersByPageAsync(request, OrderSectionEnum.NftSection,
            OrderSectionEnum.SettlementSection, OrderSectionEnum.OrderStateSection);
        return new CommonResponseDto<PagedResultDto<OrderDto>>(pager);
    }

    [Authorize(Roles = "OrderManager")]
    [HttpPost("ramp/order")]
    public async Task<CommonResponseDto<Empty>> UpdateOrder(MfaRequest<OrderDto> updateOrderRequest)
    {
        await _adminAppService.AssertMfaAsync(updateOrderRequest.GoogleTfaPin);
        return await _thirdPartOrderAppService.UpdateRampOrderAsync(updateOrderRequest.Data, updateOrderRequest.Reason);
    }

    [Authorize(Roles = "OrderManager,OrderViewer")]
    [HttpGet("treasury/orders")]
    public async Task<CommonResponseDto<PagedResultDto<TreasuryOrderDto>>> GetTreasuryOrder(
        TreasuryOrderCondition request)
    {
        var pager = await _treasuryOrderProvider.QueryOrderAsync(request);
        return new CommonResponseDto<PagedResultDto<TreasuryOrderDto>>(pager);
    }

    [Authorize(Roles = "OrderManager,OrderViewer")]
    [HttpGet("treasury/order/statusFlow")]
    public async Task<CommonResponseDto<OrderStatusInfoIndex>> GetTreasuryOrderStatusFlow(string orderId)
    {
        var pager = await _treasuryOrderProvider.QueryOrderStatusInfoPagerAsync(new List<string> { orderId });
        return new CommonResponseDto<OrderStatusInfoIndex>(pager.Items.FirstOrDefault());
    }


    [Authorize(Roles = "OrderManager,OrderViewer")]
    [HttpPost("treasury/order/export")]
    public async Task<IActionResult> ExportTreasuryOrders(TreasuryOrderExportRequest request)
    {
        try
        {
            AssertHelper.NotNull(request, "Data request");
            AssertHelper.NotNull(request.Data, "Data empty");
            AssertHelper.NotNull(request.Data.CreateTimeLt, "CreateTime start empty");
            AssertHelper.NotNull(request.Data.CreateTimeGtEq, "CreateTime end empty");
            await _adminAppService.AssertMfaAsync(request.GoogleTfaPin);
            var startTime = TimeHelper.GetDateTimeFromTimeStamp(request.Data.CreateTimeGtEq ?? 0)
                .ToZoneString(request.TimeZone, TimeHelper.DatePattern);
            var endTime = TimeHelper.GetDateTimeFromTimeStamp(request.Data.CreateTimeLt ?? 0)
                .ToZoneString(request.TimeZone, TimeHelper.DatePattern);
            var orderList = await _treasuryOrderProvider.ExportOrderAsync(request.Data);
            var orderResp = new TreasuryOrderExportResponseDto(orderList);
            return File(Encoding.UTF8.GetBytes(orderResp.ToCsvString(request.TimeZone)), "text/csv",
                string.Join(CommonConstant.Dot, "treasuryOrderExport", startTime, endTime, "csv"));
        }
        catch (UserFriendlyException e)
        {
            _logger.LogError("ExportTreasuryOrders failed: {Message}", e.Message);
            return BadRequest(e.Message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ExportTreasuryOrders error");
            return StatusCode(StatusCodes.Status502BadGateway, "Internal error, please try again later");
        }
    }

    [Authorize(Roles = "OrderManager")]
    [HttpPost("treasury/order")]
    public async Task<CommonResponseDto<Empty>> UpdateTreasuryOrder(MfaRequest<TreasuryOrderDto> updateOrderRequest)
    {
        await _adminAppService.AssertMfaAsync(updateOrderRequest.GoogleTfaPin);
        return await _thirdPartOrderAppService.UpdateTreasuryOrderAsync(updateOrderRequest.Data, updateOrderRequest.Reason);
    }
}