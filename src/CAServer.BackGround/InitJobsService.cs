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
        }
        catch (Exception e)
        {
            _logger.LogError("An exception occurred while creating recurring jobs.", e);
        }

        return Task.CompletedTask;
    }
}