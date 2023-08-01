using System.Threading.Tasks;
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

    public ThirdPartMerchantController(
        IThirdPartOrderAppService thirdPartOrderAppService,
        IOrderProcessorFactory orderProcessorFactory)
    {
        _thirdPartOrdersAppService = thirdPartOrderAppService;
        _orderProcessorFactory = orderProcessorFactory;
    }

    [HttpPost("order/alchemy")]
    public async Task<BasicOrderResult> UpdateAlchemyOrderAsync(AlchemyOrderUpdateDto input)
    {
        return await _orderProcessorFactory.GetProcessor(MerchantNameType.Alchemy.ToString()).OrderUpdate(input);
    }

    [HttpPost("order/transak")]
    public async Task<BasicOrderResult> UpdateTransakOrderAsync(TransakOrderUpdateDto input)
    {
        return await _orderProcessorFactory.GetProcessor(MerchantNameType.Transak.ToString()).OrderUpdate(input);
    }

}