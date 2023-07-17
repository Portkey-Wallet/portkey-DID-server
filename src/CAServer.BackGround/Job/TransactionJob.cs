using CAServer.Common;
using Hangfire;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;

namespace CAServer.BackGround.Job;

[Queue("transaction")]
public class TransactionJob : AsyncBackgroundJob<TransactionArgs>, ITransientDependency
{
    private readonly IContractProvider _contractProvider;
    private readonly ILogger<TransactionJob> _logger;

    public TransactionJob(IContractProvider contractProvider, ILogger<TransactionJob> logger)
    {
        _contractProvider = contractProvider;
        _logger = logger;
    }


    public override async Task ExecuteAsync(TransactionArgs args)
    {
        var output = await _contractProvider.SendRawTransaction("AELF", args.RawTransaction);
        _logger.LogInformation(output.TransactionId);
        //order status  Transferred
        
        //ach
    }
}