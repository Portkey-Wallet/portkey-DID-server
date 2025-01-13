using System;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Tokens.Dtos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace CAServer.Tokens.Cache;

public interface ITokenCacheProvider
{
    Task<GetTokenInfoDto> GetTokenInfoAsync(string chainId, string symbol, TokenType inputTokenType);
}

public class TokenCacheProvider : ITokenCacheProvider, ISingletonDependency
{
    private readonly IContractProvider _contractProvider;
    private readonly IDistributedCache<GetTokenInfoDto> _tokenInfoCache;
    private readonly ILogger<TokenCacheProvider> _logger;

    public TokenCacheProvider(IContractProvider contractProvider, IDistributedCache<GetTokenInfoDto> tokenInfoCache,
        ILogger<TokenCacheProvider> logger)
    {
        _contractProvider = contractProvider;
        _tokenInfoCache = tokenInfoCache;
        _logger = logger;
    }

    public async Task<GetTokenInfoDto> GetTokenInfoAsync(string chainId, string symbol, TokenType inputTokenType)
    {
        var tokenType = TokenHelper.GetTokenType(symbol);
        if (tokenType != inputTokenType)
        {
            return new GetTokenInfoDto();
        }

        var cacheKey = string.Format(CommonConstant.CacheTokenInfoPre, chainId, symbol);
        try
        {
            var tokenInfoCache = await _tokenInfoCache.GetAsync(cacheKey);
            if (tokenInfoCache == null)
            {
                var output = await _contractProvider.GetTokenInfoAsync(chainId, symbol);
                tokenInfoCache = output != null && output.Symbol == symbol
                    ? new GetTokenInfoDto
                    {
                        Id = chainId + "-" + symbol,
                        ChainId = chainId,
                        Symbol = output.Symbol,
                        TokenName = output.TokenName,
                        Type = nameof(tokenType),
                        Decimals = output.Decimals,
                        TotalSupply = output.TotalSupply,
                        Issuer = output.Issuer.ToBase58(),
                        IsBurnable = output.IsBurnable,
                        IssueChainId = output.IssueChainId,
                        Expires = output.ExternalInfo?.Value.TryGetValue("__seed_exp_time", out _) == true
                            ? output.ExternalInfo?.Value["__seed_exp_time"]
                            : "",
                        SeedOwnedSymbol = output.ExternalInfo?.Value.TryGetValue("__seed_owned_symbol", out _) == true
                            ? output.ExternalInfo?.Value?["__seed_owned_symbol"]
                            : "",
                        ImageUrl = output.ExternalInfo?.Value.TryGetValue("__ft_image_uri", out _) == true
                            ? output.ExternalInfo?.Value?["__ft_image_uri"]
                            : ""
                    }
                    : new GetTokenInfoDto();
                await _tokenInfoCache.SetAsync(cacheKey, tokenInfoCache, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                });
                return tokenInfoCache;
            }

            return tokenInfoCache;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CorrectTokenInfo fail: symbol={symbol}, chainId={chainId}", symbol, chainId);
            return new GetTokenInfoDto();
        }
    }
}