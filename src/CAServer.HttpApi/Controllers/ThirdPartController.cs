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
[Authorize]
public class ThirdPartOrderController : CAServerController
{
    private readonly IThirdPartOrderAppService _thirdPartOrdersAppService;
    private readonly IOrderProcessorFactory _orderProcessorFactory;
    private readonly IAlchemyServiceAppService _alchemyServiceAppService;


    public ThirdPartOrderController(
        IThirdPartOrderAppService thirdPartOrderAppService,
        IOrderProcessorFactory orderProcessorFactory, 
        IAlchemyServiceAppService alchemyServiceAppService)
    {
        _thirdPartOrdersAppService = thirdPartOrderAppService;
        _orderProcessorFactory = orderProcessorFactory;
        _alchemyServiceAppService = alchemyServiceAppService;
    }

    [HttpGet("orders")]
    public async Task<OrdersDto> GetThirdPartOrdersAsync(GetUserOrdersDto input)
    {
        return await _thirdPartOrdersAppService.GetThirdPartOrdersAsync(input);
    }

    [HttpPost("order")]
    public async Task<OrderCreatedDto> CreateThirdPartOrderAsync(
        CreateUserOrderDto input)
    {
        return await _orderProcessorFactory.GetProcessor(input.MerchantName).CreateThirdPartOrderAsync(input);
    }

    [HttpPost("{merchant}/txHash")]
    public async Task SendTxHashAsync(TransactionHashDto request, string merchant)
    {
        await _orderProcessorFactory.GetProcessor(merchant).UpdateTxHashAsync(request);
    }

    [HttpPost("{merchant}/transaction")]
    public async Task ForwardTransactionAsync(TransactionDto input, string merchant)
    {
        await _orderProcessorFactory.GetProcessor(merchant).ForwardTransactionAsync(input);
    }

    [HttpPost("alchemy/token")]
    public async Task<AlchemyTokenDto> GetAlchemyFreeLoginTokenAsync(
        GetAlchemyFreeLoginTokenDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyFreeLoginTokenAsync(input);
    }

    [HttpGet("{merchant}/fiatList")]
    public async Task<AlchemyFiatListDto> GetAlchemyFiatListAsync(GetAlchemyFiatListDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyFiatListAsync(input);
    }

    [HttpGet("{merchant}/cryptoList")]
    public async Task<AlchemyCryptoListDto> GetAchCryptoListAsync(GetAlchemyCryptoListDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyCryptoListAsync(input);
    }

    [HttpPost("{merchant}/order/quote")]
    public async Task<AlchemyOrderQuoteResultDto> GetAlchemyOrderQuoteAsync(GetAlchemyOrderQuoteDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyOrderQuoteAsync(input);
    }

    [HttpGet("{merchant}/signature")]
    public async Task<AlchemySignatureResultDto> GetAlchemySignatureAsync(GetAlchemySignatureDto input)
    {
        return await _alchemyServiceAppService.GetAlchemySignatureAsync(input);
    }
}