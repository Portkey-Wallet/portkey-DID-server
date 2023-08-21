using System.Threading.Tasks;
using CAServer.Controllers;
using CAServer.Image;
using CAServer.Image.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;



[RemoteService]
[Area("app")]
[ControllerName("Image")]
[Route("api/app/image")]
[Authorize]
public class ImageController : CAServerController
{
    private readonly IImageAppService _imageAppService;

    public ImageController(IImageAppService imageAppService)
    {
        _imageAppService = imageAppService;
    }
    
    
    [HttpGet("getThumbnail")]
    public async Task<string> GetThumbnailAsync(GetThumbnailInput input)
    {
        return await _imageAppService.GetThumbnailAsync(input);
    }
   
    
    
    
    
    
    
}