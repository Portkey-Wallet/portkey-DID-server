using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Order;
using CAServer.ThirdPart.Dtos.ThirdPart;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ThirdPart")]
[Route("api/app/thirdPart/")]
[IgnoreAntiforgeryToken]
public class ThirdPartOrderController : CAServerController
{
    private readonly ILogger<ThirdPartOrderController> _logger;
    private readonly IThirdPartOrderAppService _thirdPartOrderAppService;
    private readonly INftCheckoutService _nftCheckoutService;

    public ThirdPartOrderController(
        INftCheckoutService nftCheckoutService, 
        IThirdPartOrderAppService thirdPartOrderAppService, ILogger<ThirdPartOrderController> logger)
    {
        _nftCheckoutService = nftCheckoutService;
        _thirdPartOrderAppService = thirdPartOrderAppService;
        _logger = logger;
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
    public async Task<CommonResponseDto<Empty>> UpdateAlchemyOrderAsync(AlchemyOrderUpdateDto input)
    {
        _logger.LogInformation("Receive request of [{Uri}], body={Request}, header={Header}",
            HttpContext.Request.Path.ToString(),
            JsonConvert.SerializeObject(input),
            JsonConvert.SerializeObject(HttpContext.Request.Headers));
        return await _thirdPartOrderAppService.OrderUpdateAsync(ThirdPartNameType.Alchemy.ToString(), input);
    }
    
    [HttpPost("order/transak")]
    public async Task<CommonResponseDto<Empty>> UpdateTransakOrderAsync(TransakEventRawDataDto input)
    {
        _logger.LogInformation("Receive request of [{Uri}], body={Request}, header={Header}",
            HttpContext.Request.Path.ToString(),
            JsonConvert.SerializeObject(input),
            JsonConvert.SerializeObject(HttpContext.Request.Headers));
        return await _thirdPartOrderAppService.OrderUpdateAsync(ThirdPartNameType.Transak.ToString(), input);
    }

    [HttpPost("nftorder/alchemy")]
    public async Task<string> UpdateAlchemyNftOrderAsync(
        AlchemyNftOrderRequestDto input)
    {
        _logger.LogInformation("Receive request of [{Uri}], body={Request}, header={Header}",
            HttpContext.Request.Path.ToString(),
            Encoding.UTF8.GetString(await HttpContext.Request.Body.GetAllBytesAsync()),
            JsonConvert.SerializeObject(HttpContext.Request.Headers));
        var res = await _nftCheckoutService
            .GetProcessor(ThirdPartNameType.Alchemy.ToString())
            .UpdateThirdPartNftOrderAsync(input);
        return res.Success ? "success" : "fail";
    }
}