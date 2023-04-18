using System.ComponentModel.DataAnnotations;

namespace CAServer.Dtos;

public class HubRequestContextDto
{
    [Required] public string ClientId { get; set; }
    [Required] public string RequestId { get; set; }
}