using System.Collections.Generic;
using CAServer.Commons;
using CAServer.Commons.Etos;

namespace CAServer.CAAccount.Dtos;

public class TransactionFeeResultDto : ChainDisplayNameDto
{
    public Fee TransactionFee { get; set; }
}

public class Fee
{
    public double Ach { get; set; } = CommonConstant.DefaultAchFee;
    public double CrossChain { get; set; } = CommonConstant.DefaultCrossChainFee;
    public double Max { get; set; } = CommonConstant.DefaultMaxFee;
    public double RedPackage { get; set; } = CommonConstant.DefaultMaxFee;
    public double Etransfer { get; set; } = CommonConstant.DefaultEtransferFee;
}