using System;
using System.ComponentModel.DataAnnotations;

namespace CAServer.ThirdPart.Dtos;

public class TransactionDto
{
    [Required] public string MerchantName { get; set; }
    [Required] public Guid OrderId { get; set; }
    [Required] public string RawTransaction { get; set; }
    [Required] public string Signature { get; set; }
    [Required] public string PublicKey { get; set; }
    
}