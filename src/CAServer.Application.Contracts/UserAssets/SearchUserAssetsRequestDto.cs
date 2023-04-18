using System.ComponentModel.DataAnnotations;

namespace CAServer.UserAssets;

public class SearchUserAssetsRequestDto : GetAssetsBase
{
    [Required] [Range(1, 100)] public string KeyWord { get; set; }
}