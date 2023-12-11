using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
using CAServer.ThirdPart.Processors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ThirdPart")]
[Route("api/app/thirdPart/")]
[IgnoreAntiforgeryToken]
public class ThirdPartOrderController : CAServerController
{
    private readonly IAlchemyOrderAppService _alchemyOrderService;
    private readonly INftCheckoutService _nftCheckoutService;
    private readonly IThirdPartOrderAppService _thirdPartOrderAppService;

    public ThirdPartOrderController(IAlchemyOrderAppService alchemyOrderService,
        INftCheckoutService nftCheckoutService, IThirdPartOrderAppService thirdPartOrderAppService)
    {
        _alchemyOrderService = alchemyOrderService;
        _nftCheckoutService = nftCheckoutService;
        _thirdPartOrderAppService = thirdPartOrderAppService;
    }

    [HttpGet("tfa/generate")]
    public IActionResult GenerateAuthCode(string key, string userName, string title)
    {
        var setupCode = _thirdPartOrderAppService.GenerateGoogleAuthCode(key, userName, title);
        
        var result = """ <html><body><img src="IMAGE" alt="QR Code"><br/>CODE<body/><html/> """;
        return new ContentResult
        {
            Content = result.Replace("IMAGE", setupCode.QrCodeSetupImageUrl).Replace("CODE", setupCode.ManualEntryKey),
            ContentType = "text/html",
            StatusCode = 200
        };
    }

    [HttpGet("orders/export")]
    public async Task<IActionResult> ExportOrders(OrderExportRequestDto requestDto)
    {
        if (!_thirdPartOrderAppService.VerifyOrderExportCode(requestDto.Auth))
        {
            // 403
            return Forbid();
        }

        try
        {
            var orderList = await _thirdPartOrderAppService.ExportOrderListAsync(new GetThirdPartOrderConditionDto(0, 100)
            {
                LastModifyTimeLt = requestDto.EndTime,
                LastModifyTimeGt = requestDto.StartTime,
                StatusIn = requestDto.Status,
                TransDirectIn = new List<string> { requestDto.Type }
            }, OrderSectionEnum.NftSection, OrderSectionEnum.SettlementSection, OrderSectionEnum.OrderStateSection);

            var orderResp = new OrderExportResponseDto(orderList);

            return File(Encoding.UTF8.GetBytes(orderResp.ToCsvText()), "text/csv",
                string.Join(CommonConstant.Dot, "orderExport", requestDto.StartTime, requestDto.EndTime, "csv"));
        }
        catch (UserFriendlyException e)
        {
            return BadRequest(e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "Internal error, please try again later");
        }
    }
    
    [HttpPost("order/alchemy")]
    public async Task<BasicOrderResult> UpdateAlchemyOrderAsync(
        AlchemyOrderUpdateDto input)
    {
        return await _alchemyOrderService.UpdateAlchemyOrderAsync(input);
    }

    [HttpPost("nftorder/alchemy")]
    public async Task<string> UpdateAlchemyNftOrderAsync(
        AlchemyNftOrderRequestDto input)
    {
        var res = await _nftCheckoutService
            .GetProcessor(ThirdPartNameType.Alchemy.ToString())
            .UpdateThirdPartNftOrderAsync(input);
        return res.Success ? "success" : "fail";
    }
}