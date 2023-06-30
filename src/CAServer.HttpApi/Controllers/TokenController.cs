using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CAServer.Tokens;
using CAServer.Tokens.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Token")]
[Route("api/app/tokens")]
public class TokenController : CAServerController
{
    private readonly ITokenAppService _tokenAppService;

    public TokenController(ITokenAppService tokenAppService)
    {
        _tokenAppService = tokenAppService;
    }

    [HttpGet("prices")]
    public async Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceList(List<string> symbols)
    {
        return await _tokenAppService.GetTokenPriceListAsync(symbols);
    }

    [HttpGet("contractAddress")]
    public Task<ContractAddressDto> GetContractAddressAsync()
    {
        return _tokenAppService.GetContractAddressAsync();
    }

    [HttpGet("list")]
    public async Task<List<GetTokenListDto>> GetTokenListAsync(GetTokenListRequestDto input)
    {
        return await _tokenAppService.GetTokenListAsync(input);
    }

    [HttpGet("token")]
    public async Task<GetTokenInfoDto> GetTokenInfoAsync([Required] string chainId, [Required] string symbol)
    {
        return await _tokenAppService.GetTokenInfoAsync(chainId, symbol);
    }
}