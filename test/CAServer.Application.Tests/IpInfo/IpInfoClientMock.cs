using Moq;

namespace CAServer.IpInfo;

public partial class IpInfoServiceTest
{
    private IIpInfoClient GetIpInfoClient()
    {
        var ipInfoClient = new Mock<IIpInfoClient>();
        ipInfoClient.Setup(m => m.GetIpInfoAsync(It.IsAny<string>())).ReturnsAsync(new IpInfoDto
        {
            CountryName = "United States",
            CountryCode = "SG",
            Location = new LocationInfo
            {
                CallingCode = "1"
            }
        });

        return ipInfoClient.Object;
    }
    
}