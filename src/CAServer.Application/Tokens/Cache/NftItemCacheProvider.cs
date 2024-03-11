using System;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.UserAssets.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace CAServer.Tokens.Cache;

public interface INftItemCacheProvider
{
    Task<NftItem> GetNftItemAsync(string chainId, string symbol);
}

public class NftItemCacheProvider : INftItemCacheProvider, ISingletonDependency
{
    private readonly IContractProvider _contractProvider;
    private readonly IDistributedCache<UserAssets.Dtos.NftItem> _nftItemCache;
    private readonly ILogger<NftItemCacheProvider> _logger;

    public NftItemCacheProvider(IContractProvider contractProvider, IDistributedCache<UserAssets.Dtos.NftItem> nftItemCache, ILogger<NftItemCacheProvider> logger)
    {
        _contractProvider = contractProvider;
        _nftItemCache = nftItemCache;
        _logger = logger;
    }

    public async Task<NftItem> GetNftItemAsync(string chainId, string symbol)
    {
        var tokenType = TokenHelper.GetTokenType(symbol);
        if (tokenType != TokenType.NFTItem)
        {
            return new NftItem();
        }

        var cacheKey = string.Format(CommonConstant.CacheNftItemPre, chainId, symbol);
        try
        {
            var nftItemCache = await _nftItemCache.GetAsync(cacheKey);
            if (nftItemCache == null)
            {
                var output = await _contractProvider.GetTokenInfoAsync(chainId, symbol);
                nftItemCache = output != null && output.Symbol == symbol
                    ? new NftItem
                    {
                        ChainId = chainId,
                        Symbol = output.Symbol,
                        TokenName = output.TokenName,
                        Decimals = output.Decimals.ToString(),
                        TotalSupply = output.TotalSupply,
                        Expires = output.ExternalInfo?.Value["__seed_exp_time"],
                        SeedOwnedSymbol = output.ExternalInfo?.Value["__seed_owned_symbol"]
                    }
                    : new NftItem();
                await _nftItemCache.SetAsync(cacheKey, nftItemCache, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                });
                return nftItemCache;
            }
            return nftItemCache;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CorrectTokenInfo fail: symbol={symbol}, chainId={chainId}", symbol, chainId);
            return new NftItem();
        }
    }
}