using System.Collections.Generic;

namespace CAServer.ThirdPart.Dtos.Ramp;

public class RampPriceDto
{
    public string FiatAmount { get; set; }
    public string CryptoAmount { get; set; }
    public string ThirdPart { get; set; }
    public string Exchange { get; set; }
    public RampFeeInfo FeeInfo { get; set; }
}
