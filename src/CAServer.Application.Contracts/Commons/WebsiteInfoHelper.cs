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
                return info.website.Contains(website.website);
            }
        }

        return true;
    }


    private static List<WebsiteInfoDto> WebsiteInfoes = new List<WebsiteInfoDto>
    {
        new WebsiteInfoDto { website = "etransfer.exchange", logo = "https://icon.horse/icon/app.etransfer.exchange/50" },
        new WebsiteInfoDto { website = "ebridge.exchange", logo = "https://icon.horse/icon/ebridge.exchange/50" },
        new WebsiteInfoDto { website = "awaken.finance", logo = "https://icon.horse/icon/app.awaken.finance/50" },
        new WebsiteInfoDto { website = "aefinder.io", logo = "https://icon.horse/icon/aefinder.io/50" },
        new WebsiteInfoDto { website = "schrodingernft.ai", logo = "https://icon.horse/icon/cat.schrodingernft.ai/50" },
        new WebsiteInfoDto { website = "eforest.finance", logo = "https://icon.horse/icon/www.eforest.finance/50" },
        new WebsiteInfoDto { website = "pixiepoints.io", logo = "https://icon.horse/icon/pixiepoints.io/50" },
        new WebsiteInfoDto { website = "tmrwdao.com", logo = "https://icon.horse/icon/tmrwdao.com/50" },
        new WebsiteInfoDto { website = "ewell.finance", logo = "https://icon.horse/icon/ewell.finance/50" },
        new WebsiteInfoDto { website = "hamster.beangotown11", logo = "https://icon.horse/icon/hamster.beangotown.com/50" },
        new WebsiteInfoDto { website = "hamster.beangotown11", logo = "https://icon.horse/icon/hamster.beangotown.xyz/50" },
    };
}