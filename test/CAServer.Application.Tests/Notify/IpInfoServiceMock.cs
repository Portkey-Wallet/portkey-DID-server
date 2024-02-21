using CAServer.IpInfo;
using Moq;

namespace CAServer.Notify;

public partial class NotifyTest
{
    private IIpInfoAppService GetIpInfo()
    {
        var ipInfoService = new Mock<IIpInfoAppService>();
        ipInfoService.Setup(m => m.GetIpInfoAsync()).ReturnsAsync(new IpInfoResultDto()
        {
            Country = "Singapore",
            Code = "SG"
        });

        return ipInfoService.Object;
    }
}