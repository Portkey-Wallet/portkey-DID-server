using System;
using System.ComponentModel.DataAnnotations;

namespace CAServer.ZeroHoldings.Dtos;

public class ZeroHoldingsConfigDto
{
    [Required] public Guid UserId { get; set; }
    [Required] public string Status { get; set; }
}