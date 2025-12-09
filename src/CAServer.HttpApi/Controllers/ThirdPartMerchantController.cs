using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.Commons;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ThirdPart")]
[Route("api/app/thirdPart/merchant/")]
[IgnoreAntiforgeryToken]
public class ThirdPartMerchantController : CAServerController
{
    private readonly IThirdPartOrderAppService _thirdPartOrderAppService;

    public ThirdPartMerchantController(IThirdPartOrderAppService thirdPartOrderAppService)
    {
        _thirdPartOrderAppService = thirdPartOrderAppService;
    }

    [HttpPost("nftOrder")]
    public async Task<CommonResponseDto<CreateNftOrderResponseDto>> MerchantCreateNftOrder(CreateNftOrderRequestDto input)
    {
        return await _thirdPartOrderAppService.CreateNftOrderAsync(input);
    }

    [HttpGet("nftOrder")]
    public async Task<CommonResponseDto<NftOrderQueryResponseDto>> MerchantQueryNftOrder(OrderQueryRequestDto input)
    {
        return await _thirdPartOrderAppService.QueryMerchantNftOrderAsync(input);
    }
}