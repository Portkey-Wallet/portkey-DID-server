using System.Collections.Generic;
using CAServer.CAAccount.Dtos;

namespace CAServer.CAAccount;

public interface ITransactionFeeAppService
{
    List<TransactionFeeResultDto> CalculateFee(TransactionFeeDto input);
}