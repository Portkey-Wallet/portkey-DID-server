using System.Collections.Generic;
using CAServer.Notify.Dtos;
using CAServer.Notify.Provider;
using Moq;

namespace CAServer.Notify;

public partial class NotifyTest
{
    private INotifyProvider GetNotifyProvider()
    {
        var ipInfoService = new Mock<INotifyProvider>();
        ipInfoService.Setup(m => m.GetDataFromCms<CmsNotifyDto>(It.IsAny<string>())).ReturnsAsync(new CmsNotifyDto()
        {
            Data = new List<CmsNotify>()
            {
                new CmsNotify
                {
                    Id = 1,
                    Title = "test",
                    TargetVersion = new CmsTargetVersion
                    {
                        Value = "1.2.2"
                    },
                    AppVersions = new List<CmsAppVersion>
                    {
                        new CmsAppVersion
                        {
                            AppVersion = new CmsValue<string>
                            {
                                Value = "1.2.1"
                            }
                        }
                    }
                }
            }
        });

        return ipInfoService.Object;
    }
}