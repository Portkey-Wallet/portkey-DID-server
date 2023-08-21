using System.Threading.Tasks;
using CAServer.Image.Dto;

namespace CAServer.Image;

public interface IImageAppService
{
    
    Task<string> GetThumbnailAsync(GetThumbnailInput input);
}