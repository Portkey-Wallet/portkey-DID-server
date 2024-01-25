using System.Collections.Generic;
using JetBrains.Annotations;

namespace CAServer.ThirdPart.Dtos.ThirdPart;

public class TreasuryBaseContext
{
    
    [CanBeNull] public Dictionary<string, string> Headers { get; set; }
    
}