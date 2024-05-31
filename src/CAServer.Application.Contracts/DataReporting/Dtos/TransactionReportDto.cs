using System.ComponentModel.DataAnnotations;

namespace CAServer.DataReporting.Dtos;

public class TransactionReportDto
{
    [Required] public string ChainId { get; set; }
    [Required] public string CaAddress { get; set; }
    [Required] public string TransactionId { get; set; }
}