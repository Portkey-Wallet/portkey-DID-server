using System.ComponentModel.DataAnnotations;

namespace CAServer.Transfer.Dtos;

public class GetDepositRequestDto
{
    public string ChainId { get; set; }
    public string Network { get; set; }
    [Required] public string Symbol { get; set; }
    [Required] public string? ToSymbol { get; set; }
}