using System;
using System.Collections.Generic;
using CAServer.Device.Dtos;
using CAServer.Grains.Grain.Contacts;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NSubstitute;
using Orleans;
using Shouldly;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.Device;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class DeviceAppServiceTests : CAServerApplicationTestBase
{
    protected ICurrentUser _currentUser;
    protected IClusterClient _clusterClient;
    protected readonly IEncryptionProvider _encryptionProvider;
    protected readonly IDeviceAppService _deviceAppService;

    public DeviceAppServiceTests()
    {
        _encryptionProvider = GetRequiredService<IEncryptionProvider>();
        _deviceAppService = GetRequiredService<IDeviceAppService>();
        _clusterClient = GetRequiredService<IClusterClient>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        _currentUser = Substitute.For<ICurrentUser>();
        services.AddSingleton(_currentUser);
        services.AddSingleton(GetDeviceOptions());
        // services.AddSingleton(GetClusterClient());
    }

    private void Login(Guid userId)
    {
        _currentUser.Id.Returns(userId);
        _currentUser.IsAuthenticated.Returns(true);
    }

    [Fact]
    public void EncryptionProviderTests()
    {
        var str = "test";
        var key = "12345678901234567890123456789012";
        var salt = "1234567890123456";

        var encryptedStr = _encryptionProvider.AESEncrypt(str, key, salt);
        var decryptedStr = _encryptionProvider.AESDecrypt(encryptedStr, key, salt);

        decryptedStr.ShouldBe(str);
    }

    [Fact]
    public async void EncryptDeviceInfoAsyncTest()
    {
        Login(Guid.NewGuid());
        var result = await _deviceAppService.EncryptDeviceInfoAsync(new DeviceServiceDto
        {
            Data = new List<string> { "test" }
        });
        result.Result.Count.ShouldBe(1);

        var orgVal = await _deviceAppService.DecryptDeviceInfoAsync(new DeviceServiceDto
        {
            Data = new List<string> { result.Result[0] }
        });
        orgVal.Result.Count.ShouldBe(1);
        orgVal.Result[0].ShouldBe("test");

        orgVal = await _deviceAppService.DecryptDeviceInfoAsync(new DeviceServiceDto
            {
                Data = new List<string> { }
            }
        );
        orgVal.Result.Count.ShouldBe(0);
    }

    [Fact]
    public async void EncryptExtraDataAsyncTest()
    {
        var userId = Guid.NewGuid();
        Login(userId);
        var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var deviceData = JsonConvert.SerializeObject(new ExtraDataType
        {
            TransactionTime = time,
            DeviceInfo = "123"
        });
        var grain = _clusterClient.GetGrain<ICAHolderGrain>(userId);
        var caHash = grain.GetCAHashAsync().Result;
        var result = await _deviceAppService.EncryptExtraDataAsync(deviceData, caHash);
        var newVal = JsonConvert.DeserializeObject<ExtraDataType>(result);
        newVal.TransactionTime.ShouldBe(time);
        var orgVal = await _deviceAppService.DecryptDeviceInfoAsync(new DeviceServiceDto
        {
            Data = new List<string> { newVal.DeviceInfo },
        });
        orgVal.Result.Count.ShouldBe(1);
        orgVal.Result[0].ShouldBe("123");

        result = await _deviceAppService.EncryptExtraDataAsync(JsonConvert.SerializeObject(new ExtraDataType
        {
            TransactionTime = time,
            DeviceInfo = ""
        }), caHash);
        newVal = JsonConvert.DeserializeObject<ExtraDataType>(result);
        newVal.DeviceInfo.ShouldBe("");
    }
}