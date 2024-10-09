using System.Threading.Tasks;
using CAServer.DataReporting.Dtos;

namespace CAServer.DataReporting;

public interface IDataReportAppService
{
    Task ExitWalletAsync(ExitWalletDto input);
    Task ReportTransactionAsync(TransactionReportDto input);
    Task ReportAccountAsync(AccountReportDto input);
}