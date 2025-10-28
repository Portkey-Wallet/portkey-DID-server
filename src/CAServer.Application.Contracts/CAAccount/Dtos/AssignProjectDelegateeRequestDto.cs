using System.Collections.Generic;
using AElf.Types;
using CAServer.Commons.Etos;

namespace CAServer.CAAccount;

public class AssignProjectDelegateeRequestDto : ChainDisplayNameDto
{
    public Hash ProjectHash { get; set; }

    public Address CAAddress { get; set; }
    
    public List<DelegateInfo> DelegateInfos { get; set; }
}

public class DelegateInfo
{
    public Dictionary<string, long> Delegations { get; set; }

    public Address ContractAddress { get; set; }

    public string MethodName { get; set; }

    public bool IsUnlimitedDelegate { get; set; }
}