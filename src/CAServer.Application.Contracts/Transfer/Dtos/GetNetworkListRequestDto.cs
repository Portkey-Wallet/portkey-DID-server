using System.ComponentModel.DataAnnotations;

namespace CAServer.Transfer.Dtos;

public class GetNetworkListRequestDto
{
    [Required] public string Type { get; set; }
    [Required] public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string Address { get; set; }
}