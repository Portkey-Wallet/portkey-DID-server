using System.Threading.Tasks;
using CAServer.Image.Dto;

namespace CAServer.Image;

public interface IImageAppService
{
    
    Task<ThumbnailResponseDto> GetThumbnailAsync(GetThumbnailInput input);
    Task<string> UploadSvg(string svgMd5);
}