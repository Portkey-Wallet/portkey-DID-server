using System.ComponentModel.DataAnnotations;

namespace CAServer.Message.Dtos;

public class ScanLoginDto
{
    [Required] public string TargetClientId { get; set; }
}