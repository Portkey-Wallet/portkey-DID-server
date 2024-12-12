using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.DataReporting.Etos;
using CAServer.Entities.Es;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core.Service;

public interface IReportService
{
    Task HandleAccountReportAsync(AccountReportEto eventData);
}

public class ReportService : IReportService, ISingletonDependency
{
    private readonly INESTRepository<AccountReportIndex, string> _repository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<ReportService> _logger;

    public ReportService(INESTRepository<AccountReportIndex, string> repository, IObjectMapper objectMapper,
        ILogger<ReportService> logger)
    {
        _repository = repository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleAccountReportAsync(AccountReportEto eventData)
    {
        try
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
        catch (Exception e)
        {
            _logger.LogError(e, "[AccountReport] handle report account data error, data:{data}",
                JsonConvert.SerializeObject(eventData));
        }
    }
}