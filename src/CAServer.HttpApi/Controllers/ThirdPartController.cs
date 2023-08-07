using System;
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
    
    [HttpGet("{merchant}/fiat")]
    public async Task<AlchemyFiatListResponseDto> GetMerchantFiatListAsync(GetAlchemyFiatListDto input, string merchant)
    {
        return await _orderProcessorFactory.GetAppService(merchant).GetMerchantFiatAsync(input);
    }

    [HttpGet("{merchant}/crypto")]
    public async Task<AlchemyCryptoListResponseDto> GetMerchantCryptoListAsync(GetAlchemyCryptoListDto input, string merchant)
    {
        return await _orderProcessorFactory.GetAppService(merchant).GetMerchantCryptoAsync(input);
    }

    [HttpPost("{merchant}/price")]
    public async Task<AlchemyOrderQuoteResponseDto> GetMerchantOrderQuoteAsync(GetAlchemyOrderQuoteDto input, string merchant)
    {
        return await _orderProcessorFactory.GetAppService(merchant).GetMerchantPriceAsync(input);
    }
    
    [HttpPost("alchemy/token")]
    public async Task<AlchemyTokenResponseDto> GetAlchemyFreeLoginTokenAsync(
        GetAlchemyFreeLoginTokenDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyFreeLoginTokenAsync(input);
    }
    
    [HttpGet("alchemy/signature")]
    public async Task<AlchemySignatureResponseDto> GetAlchemySignatureAsync(GetAlchemySignatureDto input)
    {
        return await _alchemyServiceAppService.GetAlchemySignatureAsync(input);
    }
    
    [Obsolete("For compatibility with old front-end versions.")]
    [HttpGet("alchemy/fiatList")]
    public async Task<AlchemyFiatListResponseDto> GetAlchemyFiatListAsync(GetAlchemyFiatListDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyFiatListAsync(input);
    }
    
    [Obsolete("For compatibility with old front-end versions.")]
    [HttpGet("alchemy/cryptoList")]
    public async Task<AlchemyCryptoListResponseDto> GetAchCryptoListAsync(GetAlchemyCryptoListDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyCryptoListAsync(input);
    }

    [Obsolete("For compatibility with old front-end versions.")]
    [HttpPost("alchemy/order/quote")]
    public async Task<AlchemyOrderQuoteResponseDto> GetAlchemyOrderQuoteAsync(GetAlchemyOrderQuoteDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyOrderQuoteAsync(input);
    }

}