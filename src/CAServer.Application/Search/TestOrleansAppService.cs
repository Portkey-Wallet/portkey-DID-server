using System;
using System.Threading.Tasks;
using CAServer.EnumType;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.Guardian;
using CAServer.Grains.Grain.RedDot;
using CAServer.Grains.Grain.Tokens.UserTokens;
using CAServer.Grains.Grain.UserExtraInfo;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Search;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class TestOrleansAppService : ITestOrleansAppService
{
    private readonly IClusterClient _clusterClient;

    public TestOrleansAppService(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<string> TestOrleansAsync(string grainName, string grainKey)
    {
        var resultMsg = "";
        switch (grainName)
        {
            case nameof(GuardianGrain):
                resultMsg = JsonConvert.SerializeObject(await _clusterClient.GetGrain<IGuardianGrain>(grainKey).GetGuardianAsync(""));
                break;
            case nameof(UserExtraInfoGrain):
                resultMsg = JsonConvert.SerializeObject(await _clusterClient.GetGrain<IUserExtraInfoGrain>(grainKey).GetAsync());
                break;
            case nameof(RedDotGrain):
                resultMsg = JsonConvert.SerializeObject(await _clusterClient.GetGrain<IRedDotGrain>(grainKey).GetRedDotInfo(RedDotType.Referral));
                break;
            case nameof(UserTokenGrain):
                resultMsg = JsonConvert.SerializeObject(await _clusterClient.GetGrain<IUserTokenGrain>(new Guid(grainKey)).GetUserToken());
                break;
            case nameof(CAHolderGrain):
                resultMsg = JsonConvert.SerializeObject(await _clusterClient.GetGrain<ICAHolderGrain>(new Guid(grainKey)).GetCaHolder());
                break;
        }

        return resultMsg;
    }
}