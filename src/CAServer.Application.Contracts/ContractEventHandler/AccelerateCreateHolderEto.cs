using System;
using CAServer.Account;

namespace CAServer.ContractEventHandler;

public class AccelerateCreateHolderEto
{
    public AccelerateCreateHolderEto()
    {
        RegisteredTime = DateTime.Now;
    }

    public Guid Id { get; set; }
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
    public string IdentifierHash { get; set; }
    public string GrainId { get; set; }
    public DateTime RegisteredTime { get; set; }
    public string RegisterMessage { get; set; }
    public bool? RegisterSuccess { get; set; }
    public string ChainId { get; set; }
    public ManagerInfo ManagerInfo { get; set; }
}