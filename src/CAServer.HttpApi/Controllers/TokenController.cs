using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.Commons;
using CAServer.Tokens;
using CAServer.Tokens.Dtos;
using CAServer.UserAssets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenNftAppService _tokenNftAppService;

    public TokenController(ITokenAppService tokenAppService, ITokenDisplayAppService tokenDisplayAppService,
        IHttpContextAccessor httpContextAccessor, ITokenNftAppService tokenNftAppService)
    {
        _tokenAppService = tokenAppService;
        _tokenDisplayAppService = tokenDisplayAppService;
        _httpContextAccessor = httpContextAccessor;
        _tokenNftAppService = tokenNftAppService;
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
        var version = _httpContextAccessor.HttpContext?.Request.Headers["version"].ToString();
        return VersionContentHelper.CompareVersion(version, CommonConstant.NftToFtStartVersion)
            ? await _tokenNftAppService.GetTokenListAsync(input)
            : await _tokenDisplayAppService.GetTokenListAsync(input);
    }

    [Authorize, HttpGet("token")]
    public async Task<GetTokenInfoDto> GetTokenInfoAsync([Required] string chainId, [Required] string symbol)
    {
        return await _tokenAppService.GetTokenInfoAsync(chainId, symbol);
    }

    [HttpPost("allowances")]
    public async Task<GetTokenAllowancesDto> GetTokenAllowancesAsync(GetAssetsBase input)
    {
        return await _tokenAppService.GetTokenAllowancesAsync(input);
    }
}