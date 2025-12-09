using System.ComponentModel.DataAnnotations;

namespace CAServer.ThirdPart.Dtos;

public class TransactionHashDto
{
    public string MerchantName { get; set; }
    [Required] public string OrderId { get; set; }
    [Required] public string TxHash { get; set; }
}