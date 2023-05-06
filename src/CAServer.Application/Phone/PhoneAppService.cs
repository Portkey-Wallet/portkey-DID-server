using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Phone.Dtos;
using CAServer.Grains;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.Device;
using CAServer.Options;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans;
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

    public PhoneAppService(IClusterClient clusterClient, ILogger<PhoneAppService> logger,
        IOptions<PhoneInfoOptions> phoneInfoOptions)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _phoneInfoOptions = phoneInfoOptions.Value;
    }

    public async Task<PhoneInfoListDto> GetPhoneInfo()
    {
        var phone = new PhoneInfoOptions
        {
            PhoneInfo = _phoneInfoOptions.PhoneInfo,
        };
        Console.Out.Write(phone);
        var phoneInfo = new List<Dictionary<string, string>>();
        for (int i = 0; i < _phoneInfoOptions.PhoneInfo.Count; i++)
        {
            var phoneInfoDict = new Dictionary<string, string>();
            phoneInfoDict.Add("Country",_phoneInfoOptions.PhoneInfo[i].Country);
            phoneInfoDict.Add("Code",_phoneInfoOptions.PhoneInfo[i].Code);
            phoneInfoDict.Add("Iso",_phoneInfoOptions.PhoneInfo[i].Iso);
            phoneInfo.Add(phoneInfoDict); 
        } 
        
        var phoneInfoList = new PhoneInfoListDto
        {
            Data = phoneInfo
        };
        
        return phoneInfoList;
    }
}