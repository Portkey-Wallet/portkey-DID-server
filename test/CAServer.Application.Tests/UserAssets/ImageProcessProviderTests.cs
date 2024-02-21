using CAServer.UserAssets.Provider;
using Xunit;

namespace CAServer.UserAssets;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class ImageProcessProviderTests : CAServerApplicationTestBase
{
    private IImageProcessProvider _imageProcessProvider;
    public ImageProcessProviderTests()
    {
        _imageProcessProvider = GetRequiredService<IImageProcessProvider>();
    }

    [Fact]
    public async void GetResizeImage()
    {
        await _imageProcessProvider.GetResizeImageAsync("https://portkey-did.s3.ap-northeast-1/img/Untitled/5.jpg", -1 ,-1,ImageResizeType.PortKey);
        await _imageProcessProvider.GetResizeImageAsync("https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/Untitled/5.jpg", 144 ,144,ImageResizeType.PortKey);
        await _imageProcessProvider.GetResizeImageAsync("https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/Untitled/5.jpg", 200 ,200,ImageResizeType.PortKey);
        await _imageProcessProvider.GetResizeImageAsync("https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/Untitled/5.jpg", -1 ,-1,ImageResizeType.PortKey);
    }
}