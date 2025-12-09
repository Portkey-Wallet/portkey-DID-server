using System.ComponentModel.DataAnnotations;

namespace CAServer.Transfer.Dtos;

public class GetReceiveNetworkListRequestDto
{
    [Required] public string Symbol { get; set; }
}