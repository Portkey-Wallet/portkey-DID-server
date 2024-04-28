using System.Collections.Generic;

namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampDetailDto
{
    public List<ProviderRampDetailDto> ProvidersList { get; set; }
}


public class ProviderRampDetailDto
{
    public string ThirdPart { get; set; }
    public string CryptoAmount { get; set; }
    
    public string ProviderNetwork { get; set; }
    public string ProviderSymbol { get; set; }
    public string FiatAmount { get; set; }
    public string Exchange { get; set; }
    public RampFeeInfo FeeInfo { get; set; }
}