using System.Threading.Tasks;
using CAServer.DataReporting.Dtos;
using CAServer.DataReporting.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace CAServer.DataReporting;

[RemoteService(false), DisableAuditing]
public class DataReportAppService : CAServerAppService, IDataReportAppService
{
    private readonly IDistributedEventBus _distributedEventBus;

    public DataReportAppService(IDistributedEventBus distributedEventBus)
    {
        _distributedEventBus = distributedEventBus;
    }

    public async Task ExitWalletAsync(ExitWalletDto input)
    {
        await _distributedEventBus.PublishAsync(new ExitWalletEto
        {
            UserId = CurrentUser.GetId(),
            DeviceId = input.DeviceId
        });
    }

    public async Task ReportTransactionAsync(TransactionReportDto input)
    {
        Logger.LogDebug("report transaction, chainId:{chainId}, caAddress:{caAddress}, transactionId:{transactionId}",
            input.ChainId, input.CaAddress, input.TransactionId);
        await _distributedEventBus.PublishAsync(ObjectMapper.Map<TransactionReportDto, TransactionReportEto>(input));
    }
}