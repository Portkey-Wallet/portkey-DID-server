using System.Collections.Generic;

namespace CAServer.Options;

public class RampOptions
{
    public List<ThirdPartProviders> Providers { get; set; }
    public List<string> WhiteList { get; set; }
}

public class ThirdPartProviders
{
    public string Name { get; set; }
    public string Logo { get; set; }
    public string AgreementContent { get; set; }
    public List<string> PaymentTags { get; set; }
    public ProviderCoverage Coverage { get; set; }
}

public class ProviderCoverage
{
    public bool OnRamp { get; set; }
    public bool OffRamp { get; set; }
}
