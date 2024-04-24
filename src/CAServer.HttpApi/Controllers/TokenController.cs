using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CAServer.Tokens;
using CAServer.Tokens.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Token")]
[Route("api/app/tokens")]
public class TokenController : CAServerController
{
    private readonly ITokenAppService _tokenAppService;
    private readonly ITokenDisplayAppService _tokenDisplayAppService;

    public TokenController(ITokenAppService tokenAppService, ITokenDisplayAppService tokenDisplayAppService)
    {
        _tokenAppService = tokenAppService;
        _tokenDisplayAppService = tokenDisplayAppService;
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

    [Authorize, HttpGet("list")]
    public async Task<List<GetTokenListDto>> GetTokenListAsync(GetTokenListRequestDto input)
    {
        return input.Version.IsNullOrEmpty()
            ? await _tokenAppService.GetTokenListAsync(input)
            : await _tokenDisplayAppService.GetTokenListAsync(input);
    }

    [Authorize, HttpGet("token")]
    public async Task<GetTokenInfoDto> GetTokenInfoAsync([Required] string chainId, [Required] string symbol)
    {
        return await _tokenAppService.GetTokenInfoAsync(chainId, symbol);
    }
}