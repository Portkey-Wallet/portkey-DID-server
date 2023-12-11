using System.ComponentModel.DataAnnotations;

namespace CAServer.RedPackage.Dtos;

public class GenerateRedPackageInputDto
{
    [Required] public string ChainId { get; set; }
    [Required] public string Symbol { get; set; }
}