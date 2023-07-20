using System.Collections.Generic;
using CAServer.Commons;

namespace CAServer.CAAccount.Dtos;

public class TransactionFeeResultDto
{
    public string ChainId { get; set; }
    public Fee TransactionFee { get; set; }
}

public class Fee
{
    public double Ach { get; set; } = CommonConstant.DefaultAchFee;
    public double CrossChain { get; set; } = CommonConstant.DefaultCrossChainFee;
    public double Max { get; set; } = CommonConstant.DefaultMaxFee;
}