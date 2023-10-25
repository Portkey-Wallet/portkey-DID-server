using System.Collections.Generic;

namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampExchangeDto
{
    public Dictionary<string, string> Exchange { get; set; } = new ();
}

public class RampExchangeRequest
{
    public string Type { get; set; }
    public string Crypto { get; set; }
    public string Network { get; set; }
    public string Fiat { get; set; }
    public string Country { get; set; }
}