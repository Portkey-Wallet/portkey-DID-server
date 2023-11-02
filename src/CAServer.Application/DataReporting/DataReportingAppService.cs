using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.DataReporting.Dtos;
using CAServer.DataReporting.Etos;
using CAServer.Entities.Es;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.DataReporting;

[RemoteService(false), DisableAuditing]
public class DataReportingAppService : CAServerAppService, IDataReportingAppService
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly INESTRepository<FireBaseTokenIndex, string> _firebaseRepository;

    public DataReportingAppService(IDistributedEventBus distributedEventBus,
        INESTRepository<FireBaseTokenIndex, string> firebaseRepository)
    {
        _distributedEventBus = distributedEventBus;
        _firebaseRepository = firebaseRepository;
    }

    public async Task ReportDeviceInfoAsync(ReportingDto input)
    {
        var id = $"{input.UserId}-{input.DeviceId}";
        var firebaseIndex = ObjectMapper.Map<ReportingDto, FireBaseTokenIndex>(input);
        firebaseIndex.Id = id;
        firebaseIndex.PortKeyId = input.UserId;
        firebaseIndex.ModificationTime = DateTime.UtcNow;
        firebaseIndex.AppStatus = AppStatus.Foreground.ToString();

        Logger.LogInformation("ReportDeviceInfoAsync :{data}", JsonConvert.SerializeObject(firebaseIndex));
        //await _firebaseRepository.AddOrUpdateAsync(firebaseIndex);
        //await _distributedEventBus.PublishAsync(ObjectMapper.Map<Reporting, ReportingEto>(input));
    }

    public async Task ReportAppStatusAsync(ReportingDataDto input)
    {
        var id = $"{input.UserId}-{input.DeviceId}";
        // var firebaseIndex = await _firebaseRepository.GetAsync(id);
        //
        // if (firebaseIndex == null)
        // {
        //     Logger.LogWarning("firebase index not exists, id:{id}", id);
        //     return;
        // }

        // firebaseIndex.AppStatus = input.Status.ToString();
        // firebaseIndex.ModificationTime = DateTime.UtcNow;

        Logger.LogInformation("ReportAppStatusAsync :{id}, {status}", id, input.Status.ToString());
        //await _firebaseRepository.UpdateAsync(firebaseIndex);

        // await _distributedEventBus.PublishAsync(ObjectMapper.Map<ReportingData, ReportingDataEto>(input));
    }

    public async Task LogoutAsync(string deviceId, Guid userId)
    {
        var id = $"{userId}-{deviceId}";
        // var firebaseIndex = await _firebaseRepository.GetAsync(id);
        //
        // if (firebaseIndex == null)
        // {
        //     Logger.LogWarning("firebase index not exists, id:{id}", id);
        //     return;
        // }

        Logger.LogInformation("LogoutAsync :{data}", id);
        //await _firebaseRepository.DeleteAsync(firebaseIndex);
    }

    private async Task<FireBaseTokenIndex> GetAsync(string id)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<FireBaseTokenIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(id)));
        //mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<FireBaseTokenIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _firebaseRepository.GetAsync(Filter);
    }
}