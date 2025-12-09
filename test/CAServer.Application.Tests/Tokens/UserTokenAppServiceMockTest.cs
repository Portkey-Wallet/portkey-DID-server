using System.Collections.Generic;
using CAServer.Tokens.Provider;
using Moq;
using Volo.Abp.Caching;

namespace CAServer.Tokens;

public partial class UserTokenAppServiceTests
{
    private IDistributedCache<List<string>> GetMockSymbolCache()
    {
        var mockCache = new Mock<IDistributedCache<List<string>>>();

        mockCache.Setup(t => t.GetAsync(It.IsAny<string>(), default, default, default))
            .ReturnsAsync( new List<string>() { "ELF" });
        return mockCache.Object;
    }
    
    private ITokenProvider GetMockITokenProvider()
    {
        var mockTokenPriceProvider = new Mock<ITokenProvider>();
        mockTokenPriceProvider.Setup(o => o.GetTokenInfosAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<string>(),0,100))
            .ReturnsAsync(new IndexerTokens()
            {
                TokenInfo=new List<IndexerToken>(){new IndexerToken()
                {
                    Id = "AELF"
                }}
            });

        return mockTokenPriceProvider.Object;
    }
}