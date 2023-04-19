using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Grains;
using CAServer.Grains.Grain.Tokens.TokenPrice;
using CAServer.Options;
using CAServer.Tokens.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;

namespace CAServer.Tokens;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class TokenAppService : CAServerAppService, ITokenAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly ContractAddressOptions _contractAddressOptions;

    public TokenAppService(IClusterClient clusterClient, IOptions<ContractAddressOptions> contractAddressesOptions)
    {
        _clusterClient = clusterClient;
        _contractAddressOptions = contractAddressesOptions.Value;
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
            var symbolList = symbols.Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
            foreach (var symbol in symbolList)
            {
                var grainId = GrainIdHelper.GenerateGrainId(symbol);
                var grain = _clusterClient.GetGrain<ITokenPriceGrain>(grainId);
                var priceResult = await grain.GetCurrentPriceAsync(symbol);
                if (!priceResult.Success)
                {
                    throw new UserFriendlyException(priceResult.Message);
                }

                result.Add(priceResult.Data);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Get price failed. Error message:{ex.Message}");
            throw;
        }

        return new ListResultDto<TokenPriceDataDto>
        {
            Items = result
        };
    }

    public async Task<ListResultDto<TokenPriceDataDto>> GetTokenHistoryPriceDataAsync(
        List<GetTokenHistoryPriceInput> inputs)
    {
        var result = new List<TokenPriceDataDto>();
        try
        {
            foreach (var token in inputs)
            {
                var time = token.DateTime.ToString("dd-MM-yyyy");
                if (token.Symbol.IsNullOrEmpty())
                {
                    result.Add(new TokenPriceDataDto());
                    continue;
                }

                var grainId = GrainIdHelper.GenerateGrainId(token.Symbol.ToLower(), time);
                var grain = _clusterClient.GetGrain<ITokenPriceSnapshotGrain>(grainId);
                var priceResult = await grain.GetHistoryPriceAsync(token.Symbol.ToLower(), time);
                if (!priceResult.Success)
                {
                    throw new UserFriendlyException(priceResult.Message);
                }

                result.Add(priceResult.Data);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Get history price failed. Error message:{ex.Message}");
            throw;
        }

        return new ListResultDto<TokenPriceDataDto>
        {
            Items = result
        };
    }

    public Task<ContractAddressDto> GetContractAddressAsync()
    {
        return Task.FromResult(new ContractAddressDto
        {
            ContractName = _contractAddressOptions.TokenClaimAddress.ContractName,
            MainChainAddress = _contractAddressOptions.TokenClaimAddress.MainChainAddress,
            SideChainAddress = _contractAddressOptions.TokenClaimAddress.SideChainAddress
        });
    }
}