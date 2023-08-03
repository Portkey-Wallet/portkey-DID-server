using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ThirdPart")]
[Route("api/app/thirdPart/")]
[IgnoreAntiforgeryToken]
public class ThirdPartMerchantController : CAServerController
{
    private readonly IThirdPartOrderAppService _thirdPartOrdersAppService;
    private readonly IOrderProcessorFactory _orderProcessorFactory;
    private readonly ITransakServiceAppService _transakServiceAppService;

    public ThirdPartMerchantController(
        IThirdPartOrderAppService thirdPartOrderAppService,
        IOrderProcessorFactory orderProcessorFactory, ITransakServiceAppService transakServiceAppService)
    {
        _thirdPartOrdersAppService = thirdPartOrderAppService;
        _orderProcessorFactory = orderProcessorFactory;
        _transakServiceAppService = transakServiceAppService;
    }

    [HttpPost("order/alchemy")]
    public async Task<BasicOrderResult> UpdateAlchemyOrderAsync(AlchemyOrderUpdateDto input)
    {
        return await _orderProcessorFactory.GetProcessor(MerchantNameType.Alchemy.ToString()).OrderUpdate(input);
    }

    [HttpPost("order/transak")]
    public async Task<BasicOrderResult> UpdateTransakOrderAsync(TransakEventRawDataDto input)
    {
        return await _orderProcessorFactory.GetProcessor(MerchantNameType.Transak.ToString()).OrderUpdate(input);
    }

    [HttpGet("transak/accesstoken")]
    public async Task<Tuple<string, string>> GetWebhookAsync()
    {
        if (!EnvHelper.IsDevelopment())
            throw new UserFriendlyException("Operation denied");
        return await _transakServiceAppService.GetAccessTokenAsync();
    }
}