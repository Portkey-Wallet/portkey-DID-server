using System.Collections.Generic;
using CAServer.CAAccount.Dtos;
using CAServer.Commons;

namespace CAServer.Options;

public class TransactionFeeOptions
{
    public List<TransactionFeeInfo> TransactionFees { get; set; } = new List<TransactionFeeInfo>
    {
        new()
        {
            ChainId = CommonConstant.MainChainId,
            TransactionFee = new Fee()
        },
        new()
        {
            ChainId = CommonConstant.TDVVChainId,
            TransactionFee = new Fee()
        },
        new()
        {
            ChainId = CommonConstant.TDVWChainId,
            TransactionFee = new Fee()
        }
    };
}

public class TransactionFeeInfo
{
    public string ChainId { get; set; }
    public Fee TransactionFee { get; set; }
}