using System.Threading.Tasks;
using CAServer.UserSecurity.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.EntityEventHandler.Core.Worker;

public class SendingTransactionInfoByEmailAfterApprovalWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IUserSecurityProvider _userSecurityProvider;
    private readonly ILogger<SendingTransactionInfoByEmailAfterApprovalWorker> _logger;
    
    public SendingTransactionInfoByEmailAfterApprovalWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IUserSecurityProvider userSecurityProvider,
        ILogger<SendingTransactionInfoByEmailAfterApprovalWorker> logger) : base(timer, serviceScopeFactory)
    {
        _userSecurityProvider = userSecurityProvider;
        _logger = logger;
        Timer.Period = 1000 * 1000; //10 seconds
        Timer.RunOnStart = true;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var approvedList = await _userSecurityProvider.GetManagerApprovedListByCaHashAsync(string.Empty, string.Empty, string.Empty, 1000, 1010);
        if (approvedList?.CaHolderManagerApproved?.Data == null)
        {
            return;
        }
        _logger.LogDebug("SendingTransactionInfoByEmailAfterApprovalWorker approvedList:{0}", JsonConvert.SerializeObject(approvedList));
        foreach (var managerApprovedDto in approvedList.CaHolderManagerApproved.Data)
        {
            //ManagerApprovedDto
            //string ChainId
            //string CaHash
            //string Spender
            //string Symbol
            //long Amount
            //todo 没有Guardian信息和交易的类型
            //todo 没有Guardian查不到Guardian
        }
    }
}