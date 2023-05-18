using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Phone.Dtos;
using CAServer.Grains;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.Device;
using CAServer.IpInfo;
using CAServer.Options;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace CAServer.Phone;

[RemoteService(false), DisableAuditing]
public class PhoneAppService : CAServerAppService, IPhoneAppService
{
    private readonly IClusterClient _clusterClient;

    private readonly ILogger<PhoneAppService> _logger;

    private readonly PhoneInfoOptions _phoneInfoOptions;

    private readonly IIpInfoAppService _ipInfoAppService;

    public PhoneAppService(IIpInfoAppService ipInfoAppService, IClusterClient clusterClient, ILogger<PhoneAppService> logger,
        IOptions<PhoneInfoOptions> phoneInfoOptions)
    {
        _ipInfoAppService = ipInfoAppService;
        _clusterClient = clusterClient;
        _logger = logger;
        _phoneInfoOptions = phoneInfoOptions.Value;
    }

    public async Task<PhoneInfoListDto> GetPhoneInfoAsync()
    {
        var phoneInfo = new List<Dictionary<string, string>>();
        var allPhoneCode = new Dictionary<string, string>();
        for (int i = 0; i < _phoneInfoOptions.PhoneInfo.Count; i++)
        {
            var phoneInfoDict = new Dictionary<string, string>();
            phoneInfoDict.Add("country",_phoneInfoOptions.PhoneInfo[i].Country);
            phoneInfoDict.Add("code",_phoneInfoOptions.PhoneInfo[i].Code);
            phoneInfoDict.Add("iso",_phoneInfoOptions.PhoneInfo[i].Iso);
            phoneInfo.Add(phoneInfoDict); 
            allPhoneCode.Add(_phoneInfoOptions.PhoneInfo[i].Code, _phoneInfoOptions.PhoneInfo[i].Iso);
        }

        // default value
        Dictionary<string, string> locate = new Dictionary<string, string>();
        locate.Add("country",_phoneInfoOptions.Default.Country);
        locate.Add("code",_phoneInfoOptions.Default.Code);
        locate.Add("iso",_phoneInfoOptions.Default.Iso);
        
        try
        {
            // NOTE! The [code] and [iso] attributes are DIFFERENT in [IpInfoResultDto] and [PhoneInfoListDto].
            IpInfoResultDto ipLocate = await _ipInfoAppService.GetIpInfoAsync();
            if (ipLocate != null && allPhoneCode.ContainsKey(ipLocate.Iso))
            {
                locate = new Dictionary<string, string>();
                locate.Add("country", ipLocate.Country);
                locate.Add("code", ipLocate.Iso);
                locate.Add("iso", ipLocate.Code);
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