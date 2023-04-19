using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Entities.Es;
using CAServer.UserAssets.Dtos;

namespace CAServer.UserAssets.Provider;

public interface IImageProcessProvider
{
    
    string GetResizeImage(string imageUrl, int width, int height);

}