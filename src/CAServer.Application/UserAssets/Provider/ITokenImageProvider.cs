using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Options;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.UserAssets.Provider;

public interface ITokenImageProvider
{
    public Task<string> GetTokenImageAsync(string tokenSymbol, int width = 0, int height = 0);

    public Task<Dictionary<string, string>> GetTokenImagesAsync(List<string> tokenSymbols, int width = 0,
        int height = 0);
}

public class TokenImageProvider : ITokenImageProvider, ISingletonDependency
{
    public const int DefaultWidth = 144;
    public const int DefaultHeight = 144;
    private readonly TokenInfoOptions _tokenInfoOptions;
    private readonly IImageProcessProvider _imageProcessProvider;

    public TokenImageProvider(IOptions<TokenInfoOptions> tokenInfoOptions, IImageProcessProvider imageProcessProvider)
    {
        _imageProcessProvider = imageProcessProvider;
        _tokenInfoOptions = tokenInfoOptions.Value;
    }

    public async Task<string> GetTokenImageAsync(string tokenSymbol, int width, int height)
    {
        if (0 == width)
        {
            width = DefaultWidth;
        }

        if (0 == height)
        {
            height = DefaultHeight;
        }
        if (_tokenInfoOptions.TokenInfos.IsNullOrEmpty())
        {
            return string.Empty;
        }

        var tokenImageDic = _tokenInfoOptions.TokenInfos.ToDictionary(k => k.Key, v => v.Value.ImageUrl);
        var tokenImageUrl = tokenImageDic.TryGetValue(tokenSymbol, out var imageUrl) ? imageUrl : "";
        var result = await _imageProcessProvider.GetResizeImageAsync(tokenImageUrl, width, height, ImageResizeType.PortKey);
        return result;
    }

    public async Task<Dictionary<string, string>> GetTokenImagesAsync(List<string> tokenSymbols, int width, int height)
    {
        if (0 == width)
        {
            width = DefaultWidth;
        }

        if (0 == height)
        {
            height = DefaultHeight;
        }
        if (_tokenInfoOptions.TokenInfos.IsNullOrEmpty())
        {
            return new Dictionary<string, string>();
        }

        var tokenImageDic = _tokenInfoOptions.TokenInfos.ToDictionary(k => k.Key, v => v.Value.ImageUrl);
        var tokenImageDicResult = new Dictionary<string, string>();
        foreach (var  tokenSymbol in tokenSymbols)
        {
            var tokenImageUrl = tokenImageDic.TryGetValue(tokenSymbol, out var imageUrl) ? imageUrl : "";
            if (string.IsNullOrEmpty(tokenImageUrl))
            {
                continue;
            }
            var result = await _imageProcessProvider.GetResizeImageAsync(tokenImageUrl, width, height, ImageResizeType.PortKey);
            tokenImageDicResult.Add(tokenSymbol,result);
        }
        return tokenImageDicResult;
    }
}