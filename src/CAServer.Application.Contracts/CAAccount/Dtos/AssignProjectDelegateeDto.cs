using System.Collections.Generic;
using AElf.Types;
using CAServer.Commons.Etos;

namespace CAServer.CAAccount;

public class AssignProjectDelegateeDto : ChainDisplayNameDto
{
    public Hash ProjectHash { get; set; }

    public Address CAAddress { get; set; }
    
    public List<DelegateInfo> DelegateInfos { get; set; }
}

