using System.Threading.Tasks;
using CAServer.ChatBot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.EntityEventHandler.Core.Worker;

public class InitAddChatBotContactsWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IChatBotAppService _chatBotAppService;
    private readonly ILogger<InitAddChatBotContactsWorker> _logger;

    public InitAddChatBotContactsWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IChatBotAppService chatBotAppService, ILogger<InitAddChatBotContactsWorker> logger) : base(timer,
        serviceScopeFactory)
    {
        _chatBotAppService = chatBotAppService;
        _logger = logger;
        Timer.Period = WorkerConst.TimePeriod;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogDebug("Init chatBot data starting....");
        await _chatBotAppService.InitAddChatBotContactAsync();
    }
}