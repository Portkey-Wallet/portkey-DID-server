using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Processors;
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
        var result = """ <html><body><img src="IMAGE" alt="QR Code"><br/>CODE<body/><html/> """;
        var setupCode = _thirdPartOrderAppService.GenerateOrderListSetupCode(key, userName, title);
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
        if (!_thirdPartOrderAppService.VerifyOrderListCode(requestDto.Auth))
            return Unauthorized();

        var orderList = await _thirdPartOrderAppService.ExportOrderList(new GetThirdPartOrderConditionDto(0, 100)
        {
            LastModifyTimeLt = DateTime.Parse(requestDto.EndTime).ToUtcMilliSeconds().ToString(),
            LastModifyTimeGt = DateTime.Parse(requestDto.StartTime).ToUtcMilliSeconds().ToString(),
            StatusIn = requestDto.Status,
            TransDirectIn = new List<string> { requestDto.Type }
        });
        
        var orderResp = new OrderExportResponseDto
        {
            OrderList = orderList
        };

        return File(Encoding.UTF8.GetBytes(orderResp.ToCsvText()), "text/csv",
            string.Join(CommonConstant.Dot, "orderExport", requestDto.StartTime, requestDto.EndTime, "csv"));
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