using System.Threading.Tasks;
using AutoResponseWrapper.Response;
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
    private readonly IThirdPartOrderProcessorFactory _thirdPartOrderProcessorFactory;

    public ThirdPartOrderController(IAlchemyOrderAppService alchemyOrderService,
        IThirdPartOrderProcessorFactory thirdPartOrderProcessorFactory)
    {
        _alchemyOrderService = alchemyOrderService;
        _thirdPartOrderProcessorFactory = thirdPartOrderProcessorFactory;
    }


    [HttpPost("order/alchemy")]
    public async Task<BasicOrderResult> UpdateAlchemyOrderAsync(
        AlchemyOrderUpdateDto input)
    {
        return await _alchemyOrderService.UpdateAlchemyOrderAsync(input);
    }

    [HttpPost("nftorder/alchemy")]
    public async Task<ResponseDto> UpdateAlchemyNftOrderAsync(
        AlchemyNftOrderRequestDto input)
    {
        await _thirdPartOrderProcessorFactory.GetProcessor(MerchantNameType.Alchemy.ToString()).UpdateOrderAsync(input);
        return new ResponseDto().Success();
    }
}