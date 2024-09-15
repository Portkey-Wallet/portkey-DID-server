using System.Threading.Tasks;
using CAServer.DataReporting;
using CAServer.DataReporting.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("DataReport")]
[Route("api/app/report")]
public class DataReportController : CAServerController
{
    private readonly IDataReportAppService _dataReportAppService;

    public DataReportController(IDataReportAppService dataReportAppService)
    {
        _dataReportAppService = dataReportAppService;
    }

    [HttpPost("exitWallet"), Authorize]
    public async Task ExitWalletAsync(ExitWalletDto input)
    {
        await _dataReportAppService.ExitWalletAsync(input);
    }

    [HttpPost("transaction")]
    public async Task ReportTransactionAsync(TransactionReportDto input)
    {
        await _dataReportAppService.ReportTransactionAsync(input);
    }
    
    [HttpPost("account"), Authorize]
    public async Task ReportAccountAsync(AccountReportDto input)
    {
        await _dataReportAppService.ReportAccountAsync(input);
    }
}