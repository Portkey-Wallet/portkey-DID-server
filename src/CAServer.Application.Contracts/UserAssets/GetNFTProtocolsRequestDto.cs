using System.Collections.Generic;

namespace CAServer.UserAssets;

public class GetNftCollectionsRequestDto : GetAssetsBase
{
    public int Width { get; set; }
    
    public int Height { get; set; }
}