using System.ComponentModel.DataAnnotations;

namespace CAServer.Message.Dtos;

public class AlchemyTargetAddressDto
{
    [Required] public string TargetClientId { get; set; }
    [Required] public string OrderId { get; set; }
}