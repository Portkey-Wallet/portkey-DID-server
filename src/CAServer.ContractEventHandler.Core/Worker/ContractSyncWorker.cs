using System;
using System.Threading.Tasks;
using CAServer.ContractEventHandler.Core.Application;
using CAServer.RedPackage.Etos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace CAServer.ContractEventHandler.Core.Worker;

public class ContractSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IContractAppService _contractAppService;
    private readonly ContractSyncOptions _contractSyncOptions;


    public ContractSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IContractAppService contractAppService, IOptions<ContractSyncOptions> workerOptions) : base(timer,
        serviceScopeFactory)
    {
        _contractSyncOptions = workerOptions.Value;
        _contractAppService = contractAppService;
        Timer.Period = 1000 * _contractSyncOptions.Sync;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {

        await _contractAppService.QueryAndSyncAsync();
        
        // test code
        // RedPackageCreateEto eto = new RedPackageCreateEto()
        // {
        //     UserId = Guid.NewGuid(),
        //     ChainId = "AELF",
        //     SessionId = Guid.Parse("3dc8cf4e-cdc7-4c7e-9e54-46c33840aa47"),
        //     RawTransaction = "0a220a20ca0551526b0346295d5cd0338696bcafc759553f7d56a9b55ff6ecfe1fcf1aae12220a20f5c8815048f438c59da895dd19c420e8d987931583a577049ccf1b3aa62b54e1189e92d61822047a6ce51f2a0f4372656174655265645061636b657432e9020a2435376233336234322d393230612d346162662d623937382d6338343038363037346662301203454c461801200128f18ee5bff5c10e30013802428201303433366462313163396563326266663734313539313861356536613837663735386432373766303031633335643737646535376536633031346661656333343738323462333833346336323262663663383363333932666434636538323763666662303332306234653935336336326638373634356238663365343137376165644a82016231613963613431363530656637613733343762646638346434613930616339373532373333626134353130316335333632313934646664626266386363393032323665633064636235323230343135373261333164333732303566313361346135616163373963353033623237636164313363616430313963376261303134303052220a20ca0551526b0346295d5cd0338696bcafc759553f7d56a9b55ff6ecfe1fcf1aae82f10441aafabec0e84b9aab26b676d8db213fad50a6c8743ffb4a97464b3123f30755bc706cf147ade875eea1f2611f9ce999161f6e619a3558df6ad19f59347fbb4cbb00",
        //     RedPackageId = Guid.Parse("57b33b42-920a-4abf-b978-c84086074fb0")
        // };
        // await _contractAppService.CreateRedPackageAsync(eto);
    }
}