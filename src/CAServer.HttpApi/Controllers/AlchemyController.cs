using System.Threading.Tasks;
using CAServer.Alchemy;
using CAServer.Alchemy.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Alchemy")]
[Route("api/app/alchemy")]
[Authorize]
public class AlchemyController : CAServerController
{
    private readonly IAlchemyServiceAppService _alchemyServiceAppService;

    public AlchemyController(IAlchemyServiceAppService alchemyServiceAppService)
    {
        _alchemyServiceAppService = alchemyServiceAppService;
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
}