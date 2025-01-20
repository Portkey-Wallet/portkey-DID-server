using System;
using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.Controllers;
using CAServer.Image;
using CAServer.Image.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orleans.Runtime;
using Volo.Abp;

namespace CAServer.Controllers;



[RemoteService]
[Area("app")]
[ControllerName("Image")]
[Route("api/app/image")]
public class ImageController : CAServerController
{
    private readonly IImageAppService _imageAppService;
    public ImageController(IImageAppService imageAppService)
    {
        _imageAppService = imageAppService;
    }
    
    
    [HttpGet("getThumbnail")]
    [Authorize]
    public async Task<ThumbnailResponseDto> GetThumbnailAsync(GetThumbnailInput input)
    {
        return await _imageAppService.GetThumbnailAsync(input);
    }

    [HttpGet("svg/{svgMd5}.svg")]

    public async Task<IActionResult> UploadToAmazon(string svgMd5)
    {
        var res = string.Empty;
        try
        {
            res = await _imageAppService.UploadSvg(svgMd5);
        }
        catch (Exception e)
        {
            return NotFound();
        }
        return Redirect(res);
    }
    
    
}