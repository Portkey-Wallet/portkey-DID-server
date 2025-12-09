using System.Collections.Generic;

namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampCoverageDto
{
    public Dictionary<string, RampProviderDto> ThirdPart { get; set; } = new();
}

public class RampProviderDto
{
    public string Name { get; set; }
    public string AppId { get; set; }
    public string BaseUrl { get; set; }
    public string CallbackUrl { get; set; }
    public string Logo { get; set; }
    public RampProviderCoverageDto Coverage { get; set; }
    public List<string> PaymentTags { get; set; } = new ();
}

public class RampProviderCoverageDto
{
    public bool Buy { get; set; } = false;
    public bool Sell { get; set; } = false;
}