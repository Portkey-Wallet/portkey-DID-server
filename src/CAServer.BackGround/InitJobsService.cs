using CAServer.BackGround.Options;
using CAServer.BackGround.Provider;
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
                x => x.Handle(), _transactionOptions.NftOrderMerchantCallbackPeriod);
            
            // fix uncompleted NFT release result notify to ThirdPart
            _recurringJobs.AddOrUpdate<NftOrderThirdPartNftResultNotifyWorker>("HandleNftThirdPartResultNotifyAsync",
                x => x.Handle(), _transactionOptions.NftOrderThirdPartResultPeriod);
            
            // fix order status, witch is still Created/Initialized 
            _recurringJobs.AddOrUpdate<INftOrderThirdPartOrderStatusWorker>("HandleUnCompletedNftOrderPayResultRefresh",
                x => x.Handle(), _transactionOptions.HandleUnCompletedNftOrderPayResultPeriod);
            
            // fix uncompleted ELF transfer to merchant
            _recurringJobs.AddOrUpdate<INftOrderUnCompletedTransferWorker>("HandleUnCompletedNftOrderPaySuccessTransfer",
                x => x.Handle(), _transactionOptions.HandleUnCompletedNftOrderPayTransferPeriod);
        }
        catch (Exception e)
        {
            _logger.LogError("An exception occurred while creating recurring jobs.", e);
        }

        return Task.CompletedTask;
    }
}