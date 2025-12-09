using System.Threading.Tasks;
using CAServer.Image;
using CAServer.Image.Dto;
using CAServer.UserAssets.Provider;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Xunit;

namespace CAServer.CAActivity.Provider;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class ImageAppServiceTest : CAServerApplicationTestBase
{
    private readonly IImageAppService _imageAppService;
    private readonly IImageProcessProvider _imageProcessProvider;

    public ImageAppServiceTest()
    {
        _imageAppService = GetRequiredService<IImageAppService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetImageProcessProviderMock());
    }

    [Fact]
    public async Task GetThumbnailAsyncTest()
    {
        var result = await _imageAppService.GetThumbnailAsync(new GetThumbnailInput()
        {
            ImageUrl = "test",
            Height = 100,
            Width = 100
        });
        
        result.ShouldNotBeNull();
        result.ThumbnailUrl.ShouldBe("test");
    }

    private IImageProcessProvider GetImageProcessProviderMock()
    {
        var provider = new Mock<IImageProcessProvider>();

        provider.Setup(t => t.GetImResizeImageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(
            new ThumbnailResponseDto()
            {
                ThumbnailUrl = "test"
            });

        return provider.Object;
    }
}