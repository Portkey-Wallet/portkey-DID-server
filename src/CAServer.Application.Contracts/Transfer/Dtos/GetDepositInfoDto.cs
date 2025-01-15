using System.Collections.Generic;

namespace CAServer.Transfer.Dtos;

public class GetDepositInfoDto
{
    public DepositInfoDto DepositInfo { get; set; }
}

public class DepositInfoDto
{
    public string DepositAddress { get; set; }
    public string MinAmount { get; set; }
    public string MinAmountUsd { get; set; }
    public string ServiceFee { get; set; }
    public string ServiceFeeUsd { get; set; }
    public string CurrentThreshold { get; set; }
    public List<string> ExtraNotes { get; set; }
    public ExtraInfo ExtraInfo { get; set; }
}

public class ExtraInfo
{
    public decimal Slippage { get; set; }
}