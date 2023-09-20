using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.PrivacyPermission;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.EntityEventHandler.Core.Worker;

public class LoginGuardianChangeRecordReceiveWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly ChainOptions _chainOptions;
    private readonly IPrivacyPermissionAppService _privacyPermissionAppService;
    private readonly ILogger<LoginGuardianChangeRecordReceiveWorker> _logger;

    public LoginGuardianChangeRecordReceiveWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IGraphQLProvider graphQlProvider, IOptions<ChainOptions> chainOptions,
        IPrivacyPermissionAppService privacyPermissionAppService,
        ILogger<LoginGuardianChangeRecordReceiveWorker> logger) : base(
        timer,
        serviceScopeFactory)
    {
        _chainOptions = chainOptions.Value;
        _graphQlProvider = graphQlProvider;
        _privacyPermissionAppService = privacyPermissionAppService;
        _logger = logger;
        Timer.Period = 1000 * WorkerOptions.TimePeriod;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        foreach (var chainOptionsChainInfo in _chainOptions.ChainInfos)
        {
            var lastEndHeight =
                await _graphQlProvider.GetLastEndHeightAsync(chainOptionsChainInfo.Key, QueryType.LoginGuardianChangeRecord);
            var newIndexHeight = await _graphQlProvider.GetIndexBlockHeightAsync(chainOptionsChainInfo.Key);
            if (lastEndHeight >= newIndexHeight)
            {
                continue;
            }

            if (lastEndHeight == 0)
            {
                lastEndHeight = newIndexHeight - 1;
            }
            
            var queryList = await _graphQlProvider.GetLoginGuardianTransactionInfosAsync(chainOptionsChainInfo.Key, lastEndHeight, newIndexHeight);
            foreach (var queryEventDto in queryList)
            {
                _logger.LogInformation(
                    "LoginGuardianChangeRecordReceiveWorker recv event cahash:{cahash},ChangeType:{ChangeType}",
                    queryEventDto.CaHash, queryEventDto.ChangeType);
                if (queryEventDto.ChangeType == QueryLoginGuardianType.LoginGuardianRemoved ||
                    queryEventDto.ChangeType == QueryLoginGuardianType.LoginGuardianUnbound)
                {
                    await _privacyPermissionAppService.DeletePrivacyPermissionAsync(chainOptionsChainInfo.Key,
                        queryEventDto.CaHash, queryEventDto.NotLoginGuardian);
                }

            }
            
            if (lastEndHeight > 0)
            {
                await _graphQlProvider.SetLastEndHeightAsync(chainOptionsChainInfo.Key, QueryType.LoginGuardianChangeRecord, lastEndHeight);
            }
            _logger.LogInformation("LoginGuardianChangeRecordReceiveWorker sync lastEndHeight: {BlockHeight}", lastEndHeight);
        }
       
        await Task.CompletedTask;
    }
}