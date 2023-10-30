using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.ThirdPart;
using CAServer.ThirdPart.Dtos;
using CAServer.ThirdPart.Dtos.ThirdPart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ThirdPart")]
[Route("api/app/thirdPart/")]
[Authorize]
public class ThirdPartObsoleteController
{
    private readonly IAlchemyOrderAppService _alchemyOrderService;
    private readonly IAlchemyServiceAppService _alchemyServiceAppService;

    public ThirdPartObsoleteController(IAlchemyOrderAppService alchemyOrderService,
        IAlchemyServiceAppService alchemyServiceAppService)
    {
        _alchemyOrderService = alchemyOrderService;
        _alchemyServiceAppService = alchemyServiceAppService;
    }

    [Obsolete("Just for old version front-end")]
    [HttpPost("alchemy/txHash")]
    public async Task SendAlchemyTxHashAsync(SendAlchemyTxHashDto request)
    {
        await _alchemyOrderService.UpdateAlchemyTxHashAsync(request);
    }

    [Obsolete("Just for old version front-end")]
    [HttpPost("alchemy/transaction")]
    public async Task TransactionAsync(TransactionDto input)
    {
        await _alchemyOrderService.TransactionAsync(input);
    }

    [Obsolete("Just for old version front-end")]
    [HttpGet("alchemy/fiatList")]
    public async Task<AlchemyBaseResponseDto<List<AlchemyFiatDto>>> GetAlchemyFiatListAsync(GetAlchemyFiatListDto input)
    {
        return AlchemyBaseResponseDto<List<AlchemyFiatDto>>.Convert(
            await _alchemyServiceAppService.GetAlchemyFiatListAsync(input));
    }

    [Obsolete("Just for old version front-end")]
    [HttpGet("alchemy/cryptoList")]
    public async Task<AlchemyBaseResponseDto<List<AlchemyCryptoDto>>> GetAchCryptoListAsync(
        GetAlchemyCryptoListDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyCryptoListAsync(input);
    }

    [Obsolete("Just for old version front-end")]
    [HttpPost("alchemy/order/quote")]
    public async Task<AlchemyBaseResponseDto<AlchemyOrderQuoteDataDto>> GetAlchemyOrderQuoteAsync(
        GetAlchemyOrderQuoteDto input)
    {
        return await _alchemyServiceAppService.GetAlchemyOrderQuoteAsync(input);
    }

    [Obsolete("Just for old version front-end")]
    [HttpGet("alchemy/signature")]
    public async Task<AlchemySignatureResultDto> GetAlchemySignatureAsync(GetAlchemySignatureDto input)
    {
        var response = await _alchemyServiceAppService.GetAlchemySignatureAsync(input);
        AssertHelper.IsTrue(response.Success, response.Message);
        return response.Data;
    }

    [Obsolete("Just for old version front-end")]
    [HttpPost("alchemy/token")]
    public async Task<AlchemyBaseResponseDto<AlchemyTokenDataDto>> GetAlchemyFreeLoginTokenAsync(
        GetAlchemyFreeLoginTokenDto input)
    {
        return AlchemyBaseResponseDto<AlchemyTokenDataDto>.Convert(
            await _alchemyServiceAppService.GetAlchemyFreeLoginTokenAsync(input));
    }
}