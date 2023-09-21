using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Grains;
using CAServer.Grains.Grain.Device;
using CAServer.IpInfo;
using CAServer.Options;
using CAServer.Phone.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Phone;

[RemoteService(false), DisableAuditing]
public class PhoneAppService : CAServerAppService, IPhoneAppService
{


    private readonly PhoneInfoOptions _phoneInfoOptions;

    private readonly IIpInfoAppService _ipInfoAppService;
    
    
    private readonly IClusterClient _clusterClient;

    public PhoneAppService(IIpInfoAppService ipInfoAppService, 
        IOptionsSnapshot<PhoneInfoOptions> phoneInfoOptions, IClusterClient clusterClient)
    {
        _ipInfoAppService = ipInfoAppService;
        _clusterClient = clusterClient;

        _phoneInfoOptions = phoneInfoOptions.Value;
    }

    public async Task<PhoneInfoListDto> GetPhoneInfoAsync()
    {
        var grainId = GrainIdHelper.GenerateGrainId("Device", "str");
        
        var grain = _clusterClient.GetGrain<IDeviceGrain>(grainId);
        var saltAsync = await grain.GetOrGenerateSaltAsync();
        var phoneInfo = new List<Dictionary<string, string>>();
        var allPhoneCode = new Dictionary<string, Dictionary<string, string>>();
        foreach (var phone in _phoneInfoOptions.PhoneInfo)
        {
            var phoneInfoDict = new Dictionary<string, string>();
            phoneInfoDict.Add("country", phone.Country);
            phoneInfoDict.Add("code", phone.Code);
            phoneInfoDict.Add("iso", phone.Iso);
            phoneInfo.Add(phoneInfoDict);

            allPhoneCode.Add(phone.Code, phoneInfoDict);
        }

        // default value
        Dictionary<string, string> locate = new Dictionary<string, string>();

        locate.Add("country", _phoneInfoOptions.Default.Country);
        locate.Add("code", _phoneInfoOptions.Default.Code);
        locate.Add("iso", _phoneInfoOptions.Default.Iso);
        try
        {
            // NOTE! The [code] and [iso] attributes are DIFFERENT in [IpInfoResultDto] and [PhoneInfoListDto].
            IpInfoResultDto ipLocate = await _ipInfoAppService.GetIpInfoAsync();
            if (ipLocate != null && allPhoneCode.ContainsKey(ipLocate.Iso))
            {
                locate = allPhoneCode.GetValueOrDefault(ipLocate.Iso, locate);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "GetIpInfoAsync error {}", e.Message);
        }

        var phoneInfoList = new PhoneInfoListDto
        {
            Data = phoneInfo,
            LocateData = locate
        };

        return phoneInfoList;
    }
}