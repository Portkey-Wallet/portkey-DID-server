using System.Collections.Generic;

namespace CAServer.ThirdPart.Dtos;

public class RampPriceDto
{
    public string Price { get; set; }
    public Dictionary<string, string> Exchange { get; set; }
    
}