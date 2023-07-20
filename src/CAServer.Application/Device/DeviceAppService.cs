using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Device.Dtos;
using CAServer.Grains;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.Device;
using CAServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace CAServer.Device;

[RemoteService(false), DisableAuditing]
public class DeviceAppService : CAServerAppService, IDeviceAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<DeviceAppService> _logger;
    private readonly IEncryptionProvider _encryptionProvider;
    private readonly DeviceOptions _deviceOptions;

    public DeviceAppService(IClusterClient clusterClient, ILogger<DeviceAppService> logger,
        IEncryptionProvider encryptionProvider, IOptions<DeviceOptions> deviceOptions)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _encryptionProvider = encryptionProvider;
        _deviceOptions = deviceOptions.Value;
    }

    public async Task<DeviceServiceResultDto> EncryptDeviceInfoAsync(DeviceServiceDto serviceDto)
    {
        return await ProcessDeviceInfoAsync(serviceDto, Encrypt);
    }

    public async Task<DeviceServiceResultDto> DecryptDeviceInfoAsync(DeviceServiceDto serviceDto)
    {
        return await ProcessDeviceInfoAsync(serviceDto, Decrypt);
    }

    public async Task<string> EncryptExtraDataAsync(string extraData, string str)
    {
        try
        {
            var salt = await GetSaltAsync(str);
            
            var data = JsonConvert.DeserializeObject<ExtraDataType>(extraData);

            if (data.DeviceInfo.IsNullOrWhiteSpace())
            {
                _logger.LogWarning("ExtraDataEncrypt Error: device info is empty");
                return extraData;
            }

            data.DeviceInfo = Encrypt(data.DeviceInfo, salt);

            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            return JsonConvert.SerializeObject(data, jsonSerializerSettings);
        }
        catch (JsonSerializationException e)
        {
            _logger.LogError("ExtraDataEncrypt JsonSerialization Error: {extraData}", extraData);
            return extraData;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ExtraDataEncrypt Error: {extraData}", extraData);
            return TimeHelper.GetTimeStampInMilliseconds().ToString();
        }
    }

    private string Encrypt(string input, string salt)
    {
        try
        {
            return _encryptionProvider.AESEncrypt(input, _deviceOptions.Key, salt);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Encrypt Error");
            return TimeHelper.GetTimeStampInMilliseconds().ToString();
        }
    }

    private string Decrypt(string input, string salt)
    {
        try
        {
            return _encryptionProvider.AESDecrypt(input, _deviceOptions.Key, salt);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Decrypt Error");
            return TimeHelper.GetTimeStampInMilliseconds().ToString();
        }
    }

    private async Task<string> GetSaltAsync(string str)
    {
        var grainId = GrainIdHelper.GenerateGrainId("Device", str);
        var grain = _clusterClient.GetGrain<IDeviceGrain>(grainId);
        var salt = await grain.GetOrGenerateSaltAsync();

        return salt;
    }

    private delegate string DeviceDelegation(string input, string salt);

    private async Task<DeviceServiceResultDto> ProcessDeviceInfoAsync(DeviceServiceDto serviceDto,
        DeviceDelegation delegation)
    {
        var grain = _clusterClient.GetGrain<ICAHolderGrain>(CurrentUser.GetId());
        var str = grain.GetCAHashAsync().Result;

        var salt = await GetSaltAsync(str);

        var result = new List<string>();

        if (serviceDto.Data.IsNullOrEmpty())
        {
            return new DeviceServiceResultDto
            {
                Result = serviceDto.Data
            };
        }

        foreach (var info in serviceDto.Data)
        {
            if (!info.IsNullOrWhiteSpace())
            {
                result.Add(delegation(info, salt));
            }
        }

        serviceDto.Data = result;

        return new DeviceServiceResultDto
        {
            Result = serviceDto.Data
        };
    }
}