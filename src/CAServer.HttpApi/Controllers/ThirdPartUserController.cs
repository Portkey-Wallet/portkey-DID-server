using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Commons.Dtos;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.Ramp;
using CAServer.ThirdPart.Dtos.ThirdPart;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ThirdPart")]
[Route("api/app/thirdPart/")]
[Authorize]
public class ThirdPartUserController : CAServerController
{
    private readonly IAlchemyServiceAppService _alchemyServiceAppService;
    private readonly IThirdPartOrderAppService _thirdPartOrdersAppService;

    public ThirdPartUserController(IThirdPartOrderAppService thirdPartOrderAppService,
        IAlchemyServiceAppService alchemyServiceAppService)
    {
        _thirdPartOrdersAppService = thirdPartOrderAppService;
        _alchemyServiceAppService = alchemyServiceAppService;
    }


    [HttpPost("order")]
    public async Task<OrderCreatedDto> CreateThirdPartOrderAsync(
        CreateUserOrderDto input)
    {
        return await _thirdPartOrdersAppService.CreateThirdPartOrderAsync(input);
    }

    [HttpGet("orders")]
    public async Task<PageResultDto<OrderDto>> GetThirdPartOrdersAsync(GetUserOrdersDto input)
    {
        return await _thirdPartOrdersAppService.GetThirdPartOrdersAsync(input);
    }

    [HttpGet("ramp/coverage")]
    public async Task<CommonResponseDto<RampCoverageDto>> GetRampCoverage()
    {
        return await _thirdPartOrdersAppService.GetRampCoverageAsync();
    }

    [HttpGet("ramp/crypto")]
    public async Task<CommonResponseDto<RampCryptoDto>> GetRampCurrency(RampCryptoRequest request)
    {
        return await _thirdPartOrdersAppService.GetRampCryptoListAsync(request);
    }

    [HttpGet("ramp/fiat")]
    public async Task<CommonResponseDto<RampFiatDto>> GetRampFiat(RampFiatRequest rampFiatRequest)
    {
        return await _thirdPartOrdersAppService.GetRampFiatListAsync(rampFiatRequest);
    }

    [HttpGet("ramp/limit")]
    public async Task<CommonResponseDto<RampLimitDto>> GetRampLimit(RampLimitRequest request)
    {
        return await _thirdPartOrdersAppService.GetRampLimitAsync(request);
    }

    [HttpGet("ramp/exchange")]
    public async Task<CommonResponseDto<RampExchangeDto>> GetRampExchange(RampExchangeRequest request)
    {
        return await _thirdPartOrdersAppService.GetRampExchangeAsync(request);
    }

    [HttpGet("ramp/price")]
    public async Task<CommonResponseDto<RampPriceDto>> GetRampPrice(RampDetailRequest request)
    {
        return await _thirdPartOrdersAppService.GetRampPriceAsync(request);
    }

    [HttpGet("ramp/detail")]
    public async Task<CommonResponseDto<RampDetailDto>> GetRampDetail(RampDetailRequest request)
    {
        return await _thirdPartOrdersAppService.GetRampDetailAsync(request);
    }

    [HttpPost("ramp/transaction")]
    public async Task<CommonResponseDto<Empty>> TransactionForwardCall(TransactionDto input)
    {
        return await _thirdPartOrdersAppService.TransactionForwardCallAsync(input);
    }

    [HttpPost("ramp/alchemy/token")]
    public async Task<CommonResponseDto<AlchemyTokenDataDto>> GetRampThirdPartFreeLoginTokenAsync(
        GetAlchemyFreeLoginTokenDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyFreeLoginTokenAsync(input);
    }

    [HttpGet("ramp/alchemy/signature")]
    public async Task<CommonResponseDto<AlchemySignatureResultDto>> GetAlchemySignatureAsync(
        GetAlchemySignatureDto input)
    {
        return await _alchemyServiceAppService.GetAlchemySignatureAsync(input);
    }

    [HttpPost("alchemy/token/nft")]
    public async Task<AlchemyBaseResponseDto<AlchemyTokenDataDto>> GetAlchemyNftFreeLoginTokenAsync(
        GetAlchemyFreeLoginTokenDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyNftFreeLoginTokenAsync(input);
    }

    [HttpGet("alchemy/signature/api")]
    public async Task<AlchemyBaseResponseDto<string>> GetAlchemyApiSignatureAsync(Dictionary<string, string> input)
    {
        return await _alchemyServiceAppService.GetAlchemyApiSignatureAsync(input);
    }
}