using System;
using System.Collections.Generic;
using System.Text;
using CAServer.Localization;
using Volo.Abp.Application.Services;

namespace CAServer;

/* Inherit your application services from this class.
 */
public abstract class CAServerAppService : ApplicationService
{
    protected CAServerAppService()
    {
        LocalizationResource = typeof(CAServerResource);
    }
}
