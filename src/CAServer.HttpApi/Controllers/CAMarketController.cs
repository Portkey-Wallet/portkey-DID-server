using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Market;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("CAMarket")]
[Route("api/app/cryptocurrency/")]
[Authorize]
public class CAMarketController : CAServerController
{
    private readonly IMarketAppService _marketAppService;

    public CAMarketController(IMarketAppService marketAppService)
    {
        _marketAppService = marketAppService;
    }
    
    [HttpGet("list")]
    public async Task<List<MarketCryptocurrencyDto>> GetMarketCryptocurrencyDataByType(string type, string sort, string sortDir)
    {
        return await _marketAppService.GetMarketCryptocurrencyDataByType(type, sort, sortDir);
    }

    [HttpPost("mark")]
    public async Task<string> MarkUserMarketFavoriteToken([FromBody] UserCollectFavoriteTokenDto dto)
    {
        await _marketAppService.UserCollectMarketFavoriteToken(dto.Id, dto.Symbol);
        return "success";
    }
    
    [HttpPost("unmark")]
    public async Task<string> UnMarkUserMarketFavoriteToken([FromBody] UserCollectFavoriteTokenDto dto)
    {
        await _marketAppService.UserCancelMarketFavoriteToken(dto.Id, dto.Symbol);
        return "success";
    }
}