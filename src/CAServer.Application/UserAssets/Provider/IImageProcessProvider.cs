using System.Threading.Tasks;
using CAServer.Image.Dto;

namespace CAServer.UserAssets.Provider;

public interface IImageProcessProvider
{
    
    Task<string> GetResizeImageAsync(string imageUrl, int width, int height,ImageResizeType type);
    
    Task<ThumbnailResponseDto> GetImResizeImageAsync(string imageUrl, int width, int height);

    Task<string> UploadSvgAsync(string filename);
}