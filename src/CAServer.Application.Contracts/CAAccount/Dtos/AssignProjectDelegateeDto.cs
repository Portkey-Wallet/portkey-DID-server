using System.Collections.Generic;
using AElf.Types;

namespace CAServer.CAAccount;

public class AssignProjectDelegateeDto
{
    public Hash ProjectHash { get; set; }

    public Address CAAddress { get; set; }
    
    public string ChainId { get; set; }

    public List<DelegateInfo> DelegateInfos { get; set; }
}

