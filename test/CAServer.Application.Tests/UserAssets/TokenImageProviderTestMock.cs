using System.Collections.Generic;
using CAServer.Common;
using CAServer.Options;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.Options;
using Moq;
using Portkey.Contracts.CA;

namespace CAServer.UserAssets;

public partial class TokenImageProviderTest
{
    
    private IImageProcessProvider GetImageProcessProvider()
    {

        var mockImageProcessProvider = new Mock<IImageProcessProvider>();
        mockImageProcessProvider.Setup(m =>
                m.GetResizeImageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ImageResizeType>()))
            .ReturnsAsync(
                "MockImageUrl"
            );

        return mockImageProcessProvider.Object;
    }
    
    
    

    private IOptions<TokenInfoOptions> GetMockTokenInfoOptions()
    {
        var dict = new Dictionary<string, Options.TokenInfo>
        {
            ["NFT"] = new()
            {
                ImageUrl = "ImageUrl"
            },
            ["USDT"] = new()
            {
                ImageUrl = "USDTImageUrl"
            }
        };

        return new OptionsWrapper<TokenInfoOptions>(new TokenInfoOptions
        {
            TokenInfos = dict
        });
    }
    
    
    
}