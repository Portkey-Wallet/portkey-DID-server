using System.ComponentModel.DataAnnotations;

namespace CAServer.Transfer.Dtos;

public class GetSendNetworkListRequestDto
{
    [Required] public string Symbol { get; set; }
    [Required] public string ChainId { get; set; }
    [Required] public string ToAddress { get; set; }
}