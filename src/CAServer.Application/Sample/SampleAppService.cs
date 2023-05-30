using System;
using System.Threading.Tasks;
using CAServer.Grains.Grain;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Sample;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class SampleAppService : CAServerAppService, ISampleAppService
{
    private readonly IClusterClient _clusterClient;

    public SampleAppService(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<string> Hello(string from, string to) =>
        await _clusterClient.GetGrain<ISampleGrain>(0).SayHello(from, to);
}