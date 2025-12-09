using System.ComponentModel.DataAnnotations;
using CAServer.EnumType;

namespace CAServer.DataReporting.Dtos;

public class AccountReportDto
{
    public ClientType ClientType { get; set; }
    [Required] public string CaHash { get; set; }
    [Required] public string ProjectCode { get; set; }
    public AccountOperationType OperationType { get; set; }
}