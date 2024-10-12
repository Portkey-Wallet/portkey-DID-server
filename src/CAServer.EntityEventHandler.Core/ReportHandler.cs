using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using CAServer.DataReporting.Etos;
using CAServer.Entities.Es;
using CAServer.Monitor.Interceptor;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class ReportHandler : IDistributedEventHandler<AccountReportEto>, ITransientDependency
{
    private readonly INESTRepository<AccountReportIndex, string> _repository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<ReportHandler> _logger;

    public ReportHandler(INESTRepository<AccountReportIndex, string> repository, IObjectMapper objectMapper,
        ILogger<ReportHandler> logger)
    {
        _repository = repository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "ReportHandler AccountReportEto exist error",  
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionP1))
    ]
    public async Task HandleEventAsync(AccountReportEto eventData)
    {
        var index = _objectMapper.Map<AccountReportEto, AccountReportIndex>(eventData);
        var reportIndex = await _repository.GetAsync(index.Id);
        if (reportIndex == null)
        {
            index.CreateTime = DateTime.UtcNow;
        }

        await _repository.AddOrUpdateAsync(index);
        _logger.LogInformation("[AccountReport] account report info handle success, caHash:{caHash}",
            eventData.CaHash);
    }
}