using System;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
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
    private readonly IThirdPartFactory _thirdPartFactory;
    private readonly ITransakServiceAppService _transakServiceAppService;

    public ThirdPartMerchantController(
        IThirdPartOrderAppService thirdPartOrderAppService,
        IThirdPartFactory thirdPartFactory, ITransakServiceAppService transakServiceAppService)
    {
        _thirdPartOrdersAppService = thirdPartOrderAppService;
        _thirdPartFactory = thirdPartFactory;
        _transakServiceAppService = transakServiceAppService;
    }

    [HttpPost("order/alchemy")]
    public async Task<BasicOrderResult> UpdateAlchemyOrderAsync(AlchemyOrderUpdateDto input)
    {
        return await _thirdPartFactory.GetProcessor(MerchantNameType.Alchemy.ToString()).OrderUpdate(input);
    }

    [HttpPost("order/transak")]
    public async Task<BasicOrderResult> UpdateTransakOrderAsync(TransakEventRawDataDto input)
    {
        return await _thirdPartFactory.GetProcessor(MerchantNameType.Transak.ToString()).OrderUpdate(input);
    }
    
}