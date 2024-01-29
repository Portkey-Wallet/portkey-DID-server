using CAServer.Account;

namespace CAServer.ContractEventHandler;

public class AccelerateCreateHolderEto : CreateHolderEto
{
    public string ChainId { get; set; }
    public ManagerInfo ManagerInfo { get; set; }
}