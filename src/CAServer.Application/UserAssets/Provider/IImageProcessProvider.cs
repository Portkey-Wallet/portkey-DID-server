using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Entities.Es;
using CAServer.Image.Dto;
using CAServer.UserAssets.Dtos;

namespace CAServer.UserAssets.Provider;

public interface IImageProcessProvider
{
    
    Task<string> GetResizeImageAsync(string imageUrl, int width, int height,ImageResizeType type);
    
    Task<ThumbnailResponseDto> GetImResizeImageAsync(string imageUrl, int width, int height);

}