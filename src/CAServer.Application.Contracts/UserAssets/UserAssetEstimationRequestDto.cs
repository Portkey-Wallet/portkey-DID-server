using System.ComponentModel.DataAnnotations;

namespace CAServer.UserAssets;

public class UserAssetEstimationRequestDto
{

    [Required]public string ChainId { get; set; }
    [Required]public string Symbol { get; set; }
    [Required]public string Type { get; set; }
}