using CAServer.BackGround.Options;
using CAServer.BackGround.Provider;
using CAServer.BackGround.Provider.Treasury;
using Hangfire;
using Microsoft.Extensions.Options;

namespace CAServer.BackGround;

public class InitJobsService : BackgroundService
{
    private readonly IRecurringJobManager _recurringJobs;
    private readonly TransactionOptions _transactionOptions;
    private readonly ILogger<InitJobsService> _logger;

    public InitJobsService(IRecurringJobManager recurringJobs, IOptionsSnapshot<TransactionOptions> options,ILogger<InitJobsService> logger)
    {
        _recurringJobs = recurringJobs;
        _logger = logger;
        _transactionOptions = options.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _recurringJobs.AddOrUpdate<ITransactionProvider>("HandleUnCompletedOrdersAsync",
                x => x.HandleUnCompletedOrdersAsync(), _transactionOptions.RecurringPeriod);
            
            // fix uncompleted merchant callback
            _recurringJobs.AddOrUpdate<NftOrderMerchantCallbackWorker>("HandleNftOrdersCallbackAsync",
                x => x.HandleAsync(), _transactionOptions.NftOrderMerchantCallbackPeriod);
            
            // fix uncompleted NFT release result notify to ThirdPart
            _recurringJobs.AddOrUpdate<NftOrderThirdPartNftResultNotifyWorker>("HandleNftThirdPartResultNotifyAsync",
                x => x.HandleAsync(), _transactionOptions.NftOrderThirdPartResultPeriod);
            
            // fix order status, witch is still Created/Initialized 
            _recurringJobs.AddOrUpdate<NftOrderThirdPartOrderStatusWorker>("HandleUnCompletedNftOrderPayResultRefresh",
                x => x.HandleAsync(), _transactionOptions.HandleUnCompletedNftOrderPayResultPeriod);
            
            // fix uncompleted ELF transfer to merchant
            _recurringJobs.AddOrUpdate<NftOrderSettlementTransferWorker>("HandleUnCompletedNftOrderSettlementTransfer",
                x => x.HandleAsync(), _transactionOptions.HandleUnCompletedNftOrderPayTransferPeriod);
            
            // fix uncompleted ELF order count value
            _recurringJobs.AddOrUpdate<NftOrdersSettlementWorker>("NftOrdersSettlementWorker",
                x => x.HandleAsync(), _transactionOptions.NftOrdersSettlementPeriod);
            
            // fix uncompleted treasury transfer transaction confirm
            _recurringJobs.AddOrUpdate<TreasuryTxConfirmWorker>("TreasuryTxConfirmWorker",
                x => x.HandleAsync(), _transactionOptions.HandleUnCompletedTreasuryTransferPeriod);
            
            // fix uncompleted treasury thirdPart callback
            _recurringJobs.AddOrUpdate<TreasuryCallbackWorker>("TreasuryCallbackWorker",
                x => x.HandleAsync(), _transactionOptions.HandleUnCompletedTreasuryCallbackPeriod);
            
            // fix pending treasury order
            _recurringJobs.AddOrUpdate<PendingTreasuryOrderWorker>("PendingTreasuryOrderWorker",
                x => x.HandleAsync(), _transactionOptions.HandlePendingTreasuryOrderPeriod);
            
        }
        catch (Exception e)
        {
            _logger.LogError("An exception occurred while creating recurring jobs.", e);
        }

        return Task.CompletedTask;
    }
}