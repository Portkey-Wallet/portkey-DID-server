using System.Threading.Tasks;
using CAServer.DataReporting.Etos;
using CAServer.EntityEventHandler.Core.Service;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler.Core;

public class ReportHandler : IDistributedEventHandler<AccountReportEto>, ITransientDependency
{
    private readonly IReportService _reportService;

    public ReportHandler(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task HandleEventAsync(AccountReportEto eventData)
    {
        _ = _reportService.HandleAccountReportAsync(eventData);
    }
}