using System.Collections.Generic;
using CAServer.CAAccount.Dtos;

namespace CAServer.Options;

public class TransactionFeeOptions
{
    public List<TransactionFeeInfo> TransactionFees{ get; set; }
}

public class TransactionFeeInfo
{
    public string ChainId { get; set; }
    public Fee TransactionFee { get; set; }
}