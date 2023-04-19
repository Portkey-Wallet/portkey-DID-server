using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Device.Dtos;
using CAServer.Grains;
using CAServer.Grains.Grain.Device;
using CAServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
        return await ProcessDeviceInfoAsync(serviceDto, EncryptAsync);
    }

    public async Task<DeviceServiceResultDto> DecryptDeviceInfoAsync(DeviceServiceDto serviceDto)
    {
        return await ProcessDeviceInfoAsync(serviceDto, DecryptAsync);
    }

    public async Task<string> EncryptExtraDataAsync(string extraData)
    {
        try
        {
            var salt = await GetSaltAsync();

            if (extraData.IsNullOrWhiteSpace())
            {
                _logger.LogWarning("ExtraDataEncrypt Error: extra data is empty");
                return extraData;
            }

            var data = JsonConvert.DeserializeObject<ExtraDataType>(extraData);

            if (data.DeviceInfo.IsNullOrWhiteSpace())
            {
                _logger.LogWarning("ExtraDataEncrypt Error: device info is empty");
                return extraData;
            }

            data.DeviceInfo = EncryptAsync(data.DeviceInfo, salt);

            return JsonConvert.SerializeObject(data);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ExtraDataEncrypt Error: {extraData}", extraData);
            return string.Empty;
        }
    }

    private string EncryptAsync(string input, string salt)
    {
        try
        {
            return _encryptionProvider.AESEncrypt(input, _deviceOptions.Key, salt);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Encrypt Error");
            return string.Empty;
        }
    }

    private string DecryptAsync(string input, string salt)
    {
        try
        {
            return _encryptionProvider.AESDecrypt(input, _deviceOptions.Key, salt);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Decrypt Error");
            return string.Empty;
        }
    }

    private async Task<string> GetSaltAsync()
    {
        var grainId = GrainIdHelper.GenerateGrainId("Device", CurrentUser.GetId());
        var grain = _clusterClient.GetGrain<IDeviceGrain>(grainId);
        var salt = await grain.GetOrGenerateSaltAsync();

        return salt;
    }

    private delegate string DeviceDelegation(string input, string salt);

    private async Task<DeviceServiceResultDto> ProcessDeviceInfoAsync(DeviceServiceDto serviceDto,
        DeviceDelegation delegation)
    {
        var salt = await GetSaltAsync();

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