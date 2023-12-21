using System.Threading.Tasks;
using CAServer.Image.Dto;
using CAServer.UserAssets.Provider;


namespace CAServer.Image;

public class ImageAppService : CAServerAppService, IImageAppService
{
    
    private readonly IImageProcessProvider _imageProcessProvider;

    public ImageAppService(IImageProcessProvider imageProcessProvider)
    {
        _imageProcessProvider = imageProcessProvider;
    }


    public async Task<ThumbnailResponseDto> GetThumbnailAsync(GetThumbnailInput input)
    {
        return await _imageProcessProvider.GetImResizeImageAsync(input.ImageUrl, input.Width, input.Height);
    }
}
