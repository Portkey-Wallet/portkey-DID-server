using System;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace CAServer.IpInfo;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class IpInfoClientTest : CAServerApplicationTestBase
{
    private readonly IIpInfoClient _infoClient;

    public IpInfoClientTest()
    {
        _infoClient = GetService<IIpInfoClient>();
    }

    [Fact]
    public async Task GetIpInfoTest()
    {
        try
        {
            var ip = "20.246.106.227";
            var result = await _infoClient.GetIpInfoAsync(ip);
            result.CountryCode.ShouldBe("US");
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }

    }
}