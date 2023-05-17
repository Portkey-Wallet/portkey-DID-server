using System;
using System.Collections.Generic;
using CAServer.Device.Dtos;
using CAServer.Grains.Grain.Contacts;
using CAServer.Options;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NSubstitute;
using Orleans;
using Shouldly;
using Volo.Abp.Users;
using Xunit;
using Shouldly;
using Xunit;

namespace CAServer.Phone;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class PhoneInfoServiceTests : CAServerApplicationTestBase
{
    protected ICurrentUser _currentUser;
    private readonly IPhoneAppService _phoneAppService;

    public PhoneInfoServiceTests()
    {
        _phoneAppService = GetService<IPhoneAppService>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        _currentUser = Substitute.For<ICurrentUser>();
        services.AddSingleton(_currentUser);
        services.AddSingleton(GetPhoneInfoOptions());
    }

    [Fact]
    public async void PhoneInfoSuccessTest()
    {
        var res = await _phoneAppService.GetPhoneInfoAsync();
        res.Data[0]["country"].ShouldBe("12345678901234567890123456789012");
        res.Data[0]["code"].ShouldBe("sssss");
        res.Data[0]["iso"].ShouldBe("123");
    }
}                                                             
