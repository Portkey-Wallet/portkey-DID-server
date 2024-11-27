using System.Collections.Generic;
using CAServer.Contacts;

namespace CAServer.Commons;

public class WebsiteInfoHelper
{
    public static bool WebsiteValild(WebsiteInfoDto info)
    {
        foreach (var website in WebsiteInfoes)
        {
            if (website.logo.Equals(info.logo))
            {
                return website.website.Equals(info.website);
            }
        }

        return true;
    }


    private static List<WebsiteInfoDto> WebsiteInfoes = new List<WebsiteInfoDto>
    {
        new WebsiteInfoDto { website = "https://app.etransfer.exchange", logo = "https://icon.horse/icon/app.etransfer.exchange/50" },
        new WebsiteInfoDto { website = "https://ebridge.exchange", logo = "https://icon.horse/icon/ebridge.exchange/50" },
        new WebsiteInfoDto { website = "https://app.awaken.finance", logo = "https://icon.horse/icon/app.awaken.finance/50" },
        new WebsiteInfoDto { website = "https://aefinder.io", logo = "https://icon.horse/icon/aefinder.io/50" },
        new WebsiteInfoDto { website = "https://cat.schrodingernft.ai", logo = "https://icon.horse/icon/cat.schrodingernft.ai/50" },
        new WebsiteInfoDto { website = "https://www.eforest.finance", logo = "https://icon.horse/icon/www.eforest.finance/50" },
        new WebsiteInfoDto { website = "https://pixiepoints.io", logo = "https://icon.horse/icon/pixiepoints.io/50" },
        new WebsiteInfoDto { website = "https://tmrwdao.com", logo = "https://icon.horse/icon/tmrwdao.com/50" },
        new WebsiteInfoDto { website = "https://ewell.finance", logo = "https://icon.horse/icon/ewell.finance/50" },
    };
}