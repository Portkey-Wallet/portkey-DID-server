
using System.Threading.Tasks;
using CAServer.Image.Dto;
using CAServer.UserAssets.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;


namespace CAServer.Image;

[RemoteService(false), DisableAuditing]
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

    public async Task<string> UploadSvg(string svgMd5)
    {
        return await _imageProcessProvider.UploadSvgAsync(svgMd5);
        
    }
}
