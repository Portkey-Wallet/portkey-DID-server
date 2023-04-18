using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Tokens;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace CAServer.Controllers;

[RemoteService]
[ControllerName("Token")]
[Route("api/app/tokens")]
public class TokenController : AbpController
{
    private readonly ITokenAppService _tokenAppService;

    public TokenController(ITokenAppService tokenAppService)
    {
        _tokenAppService = tokenAppService;
    }

    [HttpGet]
    [Route("/prices")]
    public async Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceList(List<string> symbols)
    {
        return await _tokenAppService.GetTokenPriceListAsync(symbols);
    }

}