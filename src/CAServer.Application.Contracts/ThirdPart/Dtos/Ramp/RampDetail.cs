using System.Collections.Generic;

namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampDetailDto
{
    public List<ProviderRampDetailDto> ProvidersList { get; set; }
}


public class ProviderRampDetailDto
{
    public string ThirdPart { get; set; }
    public string Price { get; set; }
    public Dictionary<string, string> Exchange { get; set; }
}