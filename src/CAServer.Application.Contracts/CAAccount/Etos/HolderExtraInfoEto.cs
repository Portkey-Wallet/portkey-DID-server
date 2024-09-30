using System.Collections.Generic;
using CAServer.EnumType;

namespace CAServer.Etos;

public class HolderExtraInfoEto
{
    public string GrainId { get; set; }
    public AccountOperationType OperationType { get; set; }
    public Dictionary<string,object> ExtraInfo { get; set; }
}