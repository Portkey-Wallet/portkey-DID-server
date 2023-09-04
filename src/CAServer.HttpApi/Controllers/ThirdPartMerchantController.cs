using System.Threading.Tasks;
using AutoResponseWrapper.Response;
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
    public async Task<ResponseDto> MerchantCreateNftOrder(CreateNftOrderRequestDto input)
    {
        return await _thirdPartOrderAppService.CreateNftOrderAsync(input);
    }

    [HttpGet("nftOrder")]
    public async Task<ResponseDto> MerchantQueryNftOrder(OrderQueryRequestDto input)
    {
        return await _thirdPartOrderAppService.QueryMerchantNftOrderAsync(input);
    }

    [HttpPost("nftResult")]
    public async Task<ResponseDto> MerchantNftReleaseResult(NftReleaseResultRequestDto input)
    {
        return await _thirdPartOrderAppService.NoticeNftReleaseResultAsync(input);
    }
}