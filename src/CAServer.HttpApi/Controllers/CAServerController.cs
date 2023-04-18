using CAServer.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace CAServer.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class CAServerController : AbpControllerBase
{
    protected CAServerController()
    {
        LocalizationResource = typeof(CAServerResource);
    }
}