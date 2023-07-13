using Volo.Abp.AspNetCore.Mvc;

namespace MockServer;

public abstract class CAServerMockServerController : AbpControllerBase
{
    protected CAServerMockServerController()
    {
        LocalizationResource = typeof(CAServerMockServerResource);
    }
}