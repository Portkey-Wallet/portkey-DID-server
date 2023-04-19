using Moq;

namespace CAServer.IpInfo;

public partial class IpInfoServiceTest
{
    private IIpInfoClient GetIpInfoClient()
    {
        var ipInfoClient = new Mock<IIpInfoClient>();
        ipInfoClient.Setup(m => m.GetIpInfoAsync(It.IsAny<string>())).ReturnsAsync(new IpInfoDto
        {
            CountryName = "Singapore",
            CountryCode = "65",
            Location = new LocationInfo
            {
                CallingCode = "SG"
            }
        });

        return ipInfoClient.Object;
    }
    
}