using System;
using CAServer.Commons.Etos;

namespace CAServer.Account;

public class CAAccountBase : ChainDisplayNameDto
{
    public Guid Id { get; set; }
    public DateTime? CreateTime { get; set; }
    public ManagerInfo ManagerInfo { get; set; }
    public string CaHash { get; set; }
    public string CaAddress { get; set; }

}