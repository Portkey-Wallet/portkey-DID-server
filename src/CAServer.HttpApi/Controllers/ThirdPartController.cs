using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos.ThirdPart;
using Google.Protobuf.WellKnownTypes;
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
    private readonly IThirdPartOrderAppService _thirdPartOrderAppService;
    private readonly INftCheckoutService _nftCheckoutService;

    public ThirdPartOrderController(
        INftCheckoutService nftCheckoutService, 
        IThirdPartOrderAppService thirdPartOrderAppService)
    {
        _nftCheckoutService = nftCheckoutService;
        _thirdPartOrderAppService = thirdPartOrderAppService;
    }


    [HttpPost("order/alchemy")]
    public async Task<CommonResponseDto<Empty>> UpdateAlchemyOrderAsync(AlchemyOrderUpdateDto input)
    {
        return await _thirdPartOrderAppService.OrderUpdateAsync(ThirdPartNameType.Alchemy.ToString(), input);
    }
    
    [HttpPost("order/transak")]
    public async Task<CommonResponseDto<Empty>> UpdateTransakOrderAsync(TransakEventRawDataDto input)
    {
        return await _thirdPartOrderAppService.OrderUpdateAsync(ThirdPartNameType.Transak.ToString(), input);
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