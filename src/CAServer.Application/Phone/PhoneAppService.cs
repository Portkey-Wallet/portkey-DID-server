using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.IpInfo;
using CAServer.Options;
using CAServer.Phone.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Phone;

[RemoteService(false), DisableAuditing]
public class PhoneAppService : CAServerAppService, IPhoneAppService
{
    private readonly PhoneInfoOptions _phoneInfoOptions;

    private readonly IIpInfoAppService _ipInfoAppService;

    public PhoneAppService(IIpInfoAppService ipInfoAppService,
        IOptions<PhoneInfoOptions> phoneInfoOptions)
    {
        _ipInfoAppService = ipInfoAppService;

        _phoneInfoOptions = phoneInfoOptions.Value;
    }

    public async Task<PhoneInfoListDto> GetPhoneInfoAsync()
    {
        var phoneInfo = new List<Dictionary<string, string>>();
        var allPhoneCode = new Dictionary<string, Dictionary<string, string>>();
        foreach (var item in _phoneInfoOptions.PhoneInfo)
        {
            var phoneInfoDict = new Dictionary<string, string>();
            phoneInfoDict.Add("country", item.Country);
            phoneInfoDict.Add("code", item.Code);
            phoneInfoDict.Add("iso", item.Iso);
            phoneInfo.Add(phoneInfoDict);

            allPhoneCode.Add(item.Code, phoneInfoDict);
        }

        // default value
        var locate = new Dictionary<string, string>
        {
            { "country", _phoneInfoOptions.Default.Country },
            { "code", _phoneInfoOptions.Default.Code },
            { "iso", _phoneInfoOptions.Default.Iso }
        };

        try
        {
            // NOTE! The [code] and [iso] attributes are DIFFERENT in [IpInfoResultDto] and [PhoneInfoListDto].
            var ipLocate = await _ipInfoAppService.GetIpInfoAsync();
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