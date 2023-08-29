using System.Threading.Tasks;
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
    public async Task<string> UpdateAlchemyNftOrderAsync(
        AlchemyNftPartOrderRequestDto input)
    {
        var res = await _thirdPartOrderProcessorFactory
            .GetProcessor(ThirdPartNameType.Alchemy.ToString())
            .UpdateNftOrderAsync(input);
        return res.Success ? "success" : "fail";
    }
}