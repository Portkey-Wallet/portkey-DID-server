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
        await _imageProcessProvider.GetResizeImageAsync("https://portkey-did.s3.ap-northeast-1/img/Untitled/5.jpg", -1 ,-1);
        await _imageProcessProvider.GetResizeImageAsync("https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/Untitled/5.jpg", 144 ,144);
        await _imageProcessProvider.GetResizeImageAsync("https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/Untitled/5.jpg", 200 ,200);
        await _imageProcessProvider.GetResizeImageAsync("https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/Untitled/5.jpg", -1 ,-1);

        var responseDto = await _imageProcessProvider.GetImResizeImageAsync(
            "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/Untitled/5.jpg", 100,100);


    }
}