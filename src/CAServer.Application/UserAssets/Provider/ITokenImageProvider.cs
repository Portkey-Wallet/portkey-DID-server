using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace CAServer.UserAssets.Provider;

public interface ITokenImageProvider
{
    public Task<string> GetTokenImageAsync(string tokenSymbol, int width = 0, int height = 0);

    public Task<List<Dictionary<string, string>>> GetTokenImagesAsync(List<string> tokenSymbols, int width = 0,
        int height = 0);
}

public class TokenImageProvider : ITokenImageProvider, ISingletonDependency
{
    public const int DefaultWidth = 144;
    public const int DefaultHeight = 144;

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
        
        return "";
    }

    public async Task<List<Dictionary<string, string>>> GetTokenImagesAsync(List<string> tokenSymbols, int width = 0, int height = 0)
    {
        if (0 == width)
        {
            width = DefaultWidth;
        }

        if (0 == height)
        {
            height = DefaultHeight;
        }
        
    }
}