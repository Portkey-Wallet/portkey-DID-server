using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CAServer.UserAssets;

public class SearchUserAssetsRequestDto : GetAssetsBase
{
    [MaxLength(100)] public string Keyword { get; set; }
}