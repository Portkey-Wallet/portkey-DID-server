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
    private readonly INftCheckoutService _nftCheckoutService;

    public ThirdPartOrderController(IAlchemyOrderAppService alchemyOrderService,
        INftCheckoutService nftCheckoutService)
    {
        _alchemyOrderService = alchemyOrderService;
        _nftCheckoutService = nftCheckoutService;
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