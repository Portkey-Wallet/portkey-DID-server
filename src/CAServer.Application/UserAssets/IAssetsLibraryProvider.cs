using System;
using CAServer.Options;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.UserAssets;

public interface IAssetsLibraryProvider
{
    string buildSymbolImageUrl(string symbol, string imageUrl = null);
}

public class AssetsLibraryProvider : IAssetsLibraryProvider, ISingletonDependency
{
    private readonly TokenInfoOptions _tokenInfoOptions;
    private readonly AssetsInfoOptions _assetsInfoOptions;

    public AssetsLibraryProvider(IOptions<TokenInfoOptions> tokenInfoOptions,
        IOptions<AssetsInfoOptions> assetsInfoOptions)
    {
        _tokenInfoOptions = tokenInfoOptions.Value;
        _assetsInfoOptions = assetsInfoOptions.Value;
    }

    public string buildSymbolImageUrl(string symbol, string imageUrl = null)
    {
        if (!imageUrl.IsNullOrEmpty()) return imageUrl;
        if (symbol.IsNullOrWhiteSpace() || _tokenInfoOptions?.TokenInfos == null)
        {
            return string.Empty;
        }

        if (_tokenInfoOptions.TokenInfos.ContainsKey(symbol))
        {
            return _tokenInfoOptions.TokenInfos[symbol].ImageUrl;
        }

        if (_assetsInfoOptions.ImageUrlPrefix.IsNullOrWhiteSpace() ||
            _assetsInfoOptions.ImageUrlSuffix.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        return $"{_assetsInfoOptions.ImageUrlPrefix}{symbol}{_assetsInfoOptions.ImageUrlSuffix}";
    }
}