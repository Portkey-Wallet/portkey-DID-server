using System.Collections.Generic;

namespace CAServer.CAAccount.Dtos;

public class TransactionFeeResultDto
{
    public string ChainId { get; set; }
    public Fee TransactionFee { get; set; }
}

public class Fee
{
    public double Ach { get; set; }
    public double CrossChain { get; set; }
    public double Max { get; set; }
}