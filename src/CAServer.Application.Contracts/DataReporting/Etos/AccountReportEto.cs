using CAServer.EnumType;

namespace CAServer.DataReporting.Etos;

public class AccountReportEto
{
    public ClientType ClientType { get; set; }
    public string CaHash { get; set; }
    public string ProjectCode { get; set; }
    public AccountOperationType OperationType { get; set; }
}