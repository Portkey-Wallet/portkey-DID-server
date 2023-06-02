using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Message;
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
public class ThirdPartOrderController : CAServerController
{
    private readonly IAlchemyOrderAppService _alchemyOrderService;
    private readonly IThirdPartOrderAppService _thirdPartOrdersAppService;

    public ThirdPartOrderController(IAlchemyOrderAppService alchemyOrderService,
        IThirdPartOrderAppService thirdPartOrderAppService
    )
    {
        _alchemyOrderService = alchemyOrderService;
        _thirdPartOrdersAppService = thirdPartOrderAppService;
    }

    [Authorize]
    [HttpGet("orders")]
    public async Task<OrdersDto> GetThirdPartOrdersAsync(
        GetUserOrdersDto input)
    {
        return await _thirdPartOrdersAppService.GetThirdPartOrdersAsync(input);
    }

    [Authorize]
    [HttpPost("order")]
    public async Task<OrderCreatedDto> CreateThirdPartOrderAsync(
        CreateUserOrderDto input)
    {
        return await _thirdPartOrdersAppService.CreateThirdPartOrderAsync(input);
    }

    [HttpPost("order/alchemy")]
    public async Task<BasicOrderResult> UpdateAlchemyOrderAsync(
        Dictionary<string, string> input)
    {
        var inputDto = await _alchemyOrderService.VerifyAlchemySignature<AlchemyOrderUpdateDto>(input);
        return await _alchemyOrderService.UpdateAlchemyOrderAsync(inputDto);
    }

    [Authorize]
    [HttpPost("txHash")]
    public async Task UpdateAlchemyTxHashAsync(UpdateAlchemyTxHashDto request)
    {
        await _alchemyOrderService.UpdateAlchemyTxHashAsync(request);
    }
}

[RemoteService]
[Area("app")]
[ControllerName("ThirdPart")]
[Route("api/app/thirdPart/alchemy")]
[Authorize]
public class AlchemyController : CAServerController
{
    private readonly IAlchemyServiceAppService _alchemyServiceAppService;
    private readonly AlchemyHelper _alchemyHelper;

    public AlchemyController(IAlchemyServiceAppService alchemyServiceAppService,
        AlchemyHelper alchemyHelper)
    {
        _alchemyServiceAppService = alchemyServiceAppService;
        _alchemyHelper = alchemyHelper;
    }

    [HttpPost("token")]
    public async Task<AlchemyTokenDto> GetAlchemyFreeLoginTokenAsync(
        GetAlchemyFreeLoginTokenDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyFreeLoginTokenAsync(input);
    }

    [HttpGet("fiatList")]
    public async Task<AlchemyFiatListDto> GetAlchemyFiatListAsync()
    {
        return await _alchemyServiceAppService.GetAlchemyFiatListAsync();
    }

    [HttpGet("cryptoList")]
    public async Task<AlchemyCryptoListDto> GetAchCryptoListAsync(
        GetAlchemyCryptoListDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyCryptoListAsync(input);
    }

    [HttpPost("order/quote")]
    public async Task<AlchemyOrderQuoteResultDto> GetAlchemyOrderQuoteAsync(GetAlchemyOrderQuoteDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyOrderQuoteAsync(input);
    }

    [HttpGet("signature")]
    public async Task<AlchemySignatureResultDto> GetAlchemySignatureAsync(GetAlchemySignatureDto input)
    {
        return await _alchemyServiceAppService.GetAlchemySignatureAsync(input);
    }

    [HttpPost("signature")]
    public Task<AlchemySignatureResultDto> GetAlchemySignatureV2Async(GetAlchemySignatureV2Dto input)
    {
        return Task.FromResult(_alchemyServiceAppService.GetAlchemySignatureV2Async(input.SignParams));
    }
}