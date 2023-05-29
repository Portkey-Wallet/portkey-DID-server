using System.Collections.Generic;
using CAServer.Contacts;
using CAServer.Guardian;
using CAServer.Hubs;
using CAServer.IpInfo;
using Shouldly;
using Xunit;

namespace CAServer.Common;

public class ModelTest
{
    [Fact]
    public void GuardianDtoTest()
    {
        var dto = new ManagerInfoDBase
        {
            Address = string.Empty,
            ExtraData = string.Empty,
        };

        var list = new GuardianBaseListDto
        {
            Guardians = new List<GuardianInfoBase>()
        };

        var guardian = new GuardianDto
        {
            ThirdPartyEmail = string.Empty,
            IsPrivate = false,
            FirstName = string.Empty,
            LastName = string.Empty
        };

        var baseList = new GuardianBaseListDto
        {
            Guardians = new List<GuardianInfoBase>()
        };

        dto.ShouldNotBeNull();
    }

    [Fact]
    public void HubRequestTest()
    {
        var hub = new HubRequestBase
        {
            Context = new HubRequestContext()
            {
                ClientId = "test",
                RequestId = "test"
            }
        };

        var info = hub.ToString();
        info.ShouldNotBeNull();
    }

    [Fact]
    public void IpInfoTest()
    {
        var dto = new IpInfoDto
        {
            Ip = string.Empty
        };
    }
}