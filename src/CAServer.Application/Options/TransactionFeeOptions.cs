using System.Collections.Generic;
using CAServer.CAAccount.Dtos;
using CAServer.Commons;

namespace CAServer.Options;

public class TransactionFeeOptions
{
    public List<TransactionFeeInfo> TransactionFees { get; set; } 
    //     = new()
    // {
    //     new TransactionFeeInfo
    //     {
    //         ChainId = CommonConstant.MainChainId,
    //         TransactionFee = new Fee()
    //     },
    //     new TransactionFeeInfo
    //     {
    //         ChainId = CommonConstant.TDVVChainId,
    //         TransactionFee = new Fee()
    //     },
    //     new TransactionFeeInfo
    //     {
    //         ChainId = CommonConstant.TDVWChainId,
    //         TransactionFee = new Fee()
    //     }
    // };
}

public class TransactionFeeInfo
{
    public string ChainId { get; set; }
    public Fee TransactionFee { get; set; }
}