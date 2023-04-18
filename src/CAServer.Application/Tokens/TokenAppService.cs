using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Grains.Grain.Tokens;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.Application.Dtos;

namespace CAServer.Tokens;

public class TokenAppService : CAServerAppService, ITokenAppService
{
    private readonly IClusterClient _clusterClient;

    public TokenAppService(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }
    public async Task<ListResultDto<TokenPriceDataDto>> GetTokenPriceListAsync(List<string> symbols)
    {
        var result = new List<TokenPriceDataDto>();
        if (symbols.Count == 0)
        {
            return new ListResultDto<TokenPriceDataDto>();
        }

        try
        {
            var symbolList = symbols.Select(s=>s.ToLower()).Distinct().ToList();
            foreach (var symbol in symbolList)
            {
                var grain = _clusterClient.GetGrain<ITokenPriceGrain>(symbol);
                var tokenPrice = await grain.GetCurrentPriceAsync(symbol);
                result.Add(tokenPrice);
            }
        }
        catch (Exception e)
        {
            Logger.LogError($"Get price failed. Error message:{e.Message}");
            throw;
        }
        return new ListResultDto<TokenPriceDataDto>
        {
            Items = result
        };
    }

    public async Task<TokenPriceDataDto> GetTokenHistoryPriceDataAsync(string symbol, DateTime dateTime)
    {
        TokenPriceDataDto result;
        try
        {
            var grain = _clusterClient.GetGrain<ITokenPriceSnapshotGrain>(symbol);
            result = await grain.GetHistoryPriceAsync(symbol,dateTime);
        }
        catch (Exception e)
        {
            Logger.LogError($"Get price failed. Error message:{e.Message}");
            throw;
        }
        return result;

    }
}