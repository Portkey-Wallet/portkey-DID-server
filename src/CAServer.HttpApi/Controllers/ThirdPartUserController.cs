using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace CAServer.Controllers;


[RemoteService]
[Area("app")]
[ControllerName("ThirdPart")]
[Route("api/app/thirdPart/")]
[Authorize]
public class ThirdPartUserController : CAServerController
{
    private readonly IAlchemyOrderAppService _alchemyOrderService;
    private readonly IAlchemyServiceAppService _alchemyServiceAppService;
    private readonly IThirdPartOrderAppService _thirdPartOrdersAppService;

    public ThirdPartUserController(IAlchemyOrderAppService alchemyOrderService,
        IThirdPartOrderAppService thirdPartOrderAppService, IAlchemyServiceAppService alchemyServiceAppService)
    {
        _alchemyOrderService = alchemyOrderService;
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
    public async Task<PagedResultDto<OrderDto>> GetThirdPartOrdersAsync(GetUserOrdersDto input)
    {
        return await _thirdPartOrdersAppService.GetThirdPartOrdersAsync(input);
    }
    
    [HttpPost("alchemy/txHash")]
    public async Task SendAlchemyTxHashAsync(SendAlchemyTxHashDto request)
    {
        await _alchemyOrderService.UpdateAlchemyTxHashAsync(request);
    }
    
    [HttpPost("alchemy/transaction")]
    public async Task TransactionAsync(TransactionDto input)
    {
        await _alchemyOrderService.TransactionAsync(input);
    }

    [HttpPost("alchemy/token")]
    public async Task<AlchemyBaseResponseDto<AlchemyTokenDataDto>> GetAlchemyFreeLoginTokenAsync(
        GetAlchemyFreeLoginTokenDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyFreeLoginTokenAsync(input);
    }

    [HttpPost("alchemy/token/nft")]
    public async Task<AlchemyBaseResponseDto<AlchemyTokenDataDto>> GetAlchemyNftFreeLoginTokenAsync(
        GetAlchemyFreeLoginTokenDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyNftFreeLoginTokenAsync(input);
    }
    [HttpGet("alchemy/fiatList")]
    public async Task<AlchemyBaseResponseDto<List<AlchemyFiatDto>>> GetAlchemyFiatListAsync(GetAlchemyFiatListDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyFiatListAsync(input);
    }
    
    [HttpGet("alchemy/cryptoList")]
    public async Task<AlchemyBaseResponseDto<List<AlchemyCryptoDto>>> GetAchCryptoListAsync(GetAlchemyCryptoListDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyCryptoListAsync(input);
    }

    [HttpPost("alchemy/order/quote")]
    public async Task<AlchemyBaseResponseDto<AlchemyOrderQuoteDataDto>> GetAlchemyOrderQuoteAsync(GetAlchemyOrderQuoteDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyOrderQuoteAsync(input);
    }

    [HttpGet("alchemy/signature")]
    public async Task<AlchemySignatureResultDto> GetAlchemySignatureAsync(GetAlchemySignatureDto input)
    {
        return await _alchemyServiceAppService.GetAlchemySignatureAsync(input);
    }
    
    [HttpGet("alchemy/signature/api")]
    public async Task<AlchemyBaseResponseDto<string>> GetAlchemyApiSignatureAsync(Dictionary<string, string> input)
    {
        return await _alchemyServiceAppService.GetAlchemyApiSignatureAsync(input);
    }
    
}