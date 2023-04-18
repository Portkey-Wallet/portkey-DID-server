using System.ComponentModel.DataAnnotations;

namespace CAServer.UserAssets;

public class GetNftItemsRequestDto : GetAssetsBase
{
    [Required][MinLength(1)] public string Symbol { get; set; }
}