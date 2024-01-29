using CAServer.Account;

namespace CAServer.ContractEventHandler;

public class AccelerateSocialRecoveryEto : SocialRecoveryEto
{
    public string ChainId { get; set; }
    public ManagerInfo ManagerInfo { get; set; }
}