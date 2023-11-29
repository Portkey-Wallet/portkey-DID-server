using System;
using CAServer.Options;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.UserAssets;

public interface IAssetsLibraryProvider
{
    string buildSymbolImageUrl(string symbol);
}

public class AssetsLibraryProvider : IAssetsLibraryProvider, ISingletonDependency
{
    private readonly AssetsInfoOptions _assetsInfoOptions;

    public AssetsLibraryProvider(IOptions<AssetsInfoOptions> assetsInfoOptions)
    {
        _assetsInfoOptions = assetsInfoOptions.Value;
    }

    public string buildSymbolImageUrl(string symbol)
    {
        if (symbol.IsNullOrWhiteSpace())
        {
            return String.Empty;
        }

        return $"{_assetsInfoOptions.ImageUrlPrefix}{symbol}{_assetsInfoOptions.ImageUrlSuffix}";
    }
}