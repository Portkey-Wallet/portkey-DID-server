using System.Collections.Generic;
using System.Linq;
using CAServer.CAAccount.Dtos;
using CAServer.Options;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.CAAccount;

[RemoteService(false)]
[DisableAuditing]
public class TransactionFeeAppService : CAServerAppService, ITransactionFeeAppService
{
    private readonly TransactionFeeOptions _transactionFeeOptions;

    public TransactionFeeAppService(IOptionsSnapshot<TransactionFeeOptions> transactionFeeOptions)
    {
        _transactionFeeOptions = transactionFeeOptions.Value;
    }

    public List<TransactionFeeResultDto> CalculateFee(TransactionFeeDto input)
    {
        var feeInfos = _transactionFeeOptions.GetTransactionFees().Where(t => input.ChainIds.Contains(t.ChainId)).ToList();
        return ObjectMapper.Map<List<TransactionFeeInfo>, List<TransactionFeeResultDto>>(feeInfos);
    }
}