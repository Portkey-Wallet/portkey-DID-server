using System.Collections.Generic;

namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampPriceDto
{
    public string Price { get; set; }
    public string ThirdPart { get; set; }
    public Dictionary<string, string> Exchange { get; set; }
    
}