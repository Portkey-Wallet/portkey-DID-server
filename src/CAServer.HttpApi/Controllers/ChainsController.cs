using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CAServer.Chain;
using CAServer.Commons.Etos;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Chains")]
[Route("api/app/chains")]
[Authorize(Roles = "admin")]
public class ChainsController : CAServerController
{
    private readonly IChainAppService _chainsService;

    public ChainsController(IChainAppService chainsService)
    {
        _chainsService = chainsService;
    }

    [HttpPost]
    public async Task<ChainResultDto> AddChainAsync(CreateUpdateChainDto input)
    {
        return await _chainsService.CreateAsync(input);
    }

    [HttpPut, Route("{id}")]
    public async Task<ChainResultDto> UpdateChainAsync(string id, CreateUpdateChainDto input)
    {
        return await _chainsService.UpdateAsync(id, input);
    }

    [HttpDelete, Route("{id}")]
    public async Task DeleteChainAsync(string id)
    {
        await _chainsService.DeleteAsync(id);
    }

    [HttpGet("display/info")]
    [AllowAnonymous]
    public async Task<Dictionary<string, ChainDisplayNameDto>> ListChainDisplayInfos(string chainId)
    {
        return await _chainsService.ListChainDisplayInfos(chainId);
    } 
}