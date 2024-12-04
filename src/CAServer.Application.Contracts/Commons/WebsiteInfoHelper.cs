using System.Collections.Generic;
using CAServer.Contacts;

namespace CAServer.Commons;

public class WebsiteInfoHelper
{
    public static bool WebsiteValild(WebsiteInfoDto info)
    {
        info.website = info.website.Replace("https://", "");

        foreach (var pixiepoint in Pixiepoints)
        {
            if (info.website.EndsWith(pixiepoint.website))
            {
                return info.logo.Equals(pixiepoint.logo);
            }
        }

        foreach (var website in WebsiteInfoes)
        {
            if (info.website.Equals(website.website))
            {
                return info.logo.Equals(website.logo);
            }
        }

        foreach (var website in WebsiteInfoes)
        {
            if (info.website.Contains(website.website))
            {
                return info.logo.Contains(info.website);
            }
        }
        
        return true;
    }


    private static List<WebsiteInfoDto> WebsiteInfoes = new List<WebsiteInfoDto>
    {
        new WebsiteInfoDto { website = "etransfer.exchange", logo = "https://icon.horse/icon/app.etransfer.exchange/50" },
        new WebsiteInfoDto { website = "test.etransfer.exchange", logo = "https://icon.horse/icon/test.etransfer.exchange/501" },
        new WebsiteInfoDto { website = "ebridge.exchange", logo = "https://icon.horse/icon/ebridge.exchange/50" },
        new WebsiteInfoDto { website = "test.ebridge.exchange11", logo = "https://icon.horse/icon/test.ebridge.exchange/50" },
        new WebsiteInfoDto { website = "awaken.finance", logo = "https://icon.horse/icon/app.awaken.finance/50" },
        new WebsiteInfoDto { website = "test-app.awaken.finance", logo = "https://icon.horse/icon/test-app.awaken.finance/501" },
        new WebsiteInfoDto { website = "aefinder.io", logo = "https://icon.horse/icon/aefinder.io/50" },
        new WebsiteInfoDto { website = "schrodingernft.ai", logo = "https://icon.horse/icon/cat.schrodingernft.ai/50" },
        new WebsiteInfoDto { website = "eforest.finance", logo = "https://icon.horse/icon/www.eforest.finance/50" },
        new WebsiteInfoDto { website = "pixiepoints.io", logo = "https://icon.horse/icon/pixiepoints.io/50" },
        new WebsiteInfoDto { website = "tmrwdao.com", logo = "https://icon.horse/icon/tmrwdao.com/50" },
        new WebsiteInfoDto { website = "ewell.finance", logo = "https://icon.horse/icon/ewell.finance/50" },
        new WebsiteInfoDto { website = "hamster.beangotown", logo = "https://icon.horse/icon/hamster.beangotown.com/50" },
    };

    private static List<WebsiteInfoDto> Pixiepoints = new List<WebsiteInfoDto>
    {
        new WebsiteInfoDto { website = "schrodingerai.com" ,logo = "https://icon.horse/icon/cat.schrodingerai.com/50"},
        new WebsiteInfoDto { website = "ecoearn.cc",logo = "https://icon.horse/icon/app.ecoearn.cc/50"},
        new WebsiteInfoDto { website = "awakenswap.xyz",logo = "https://icon.horse/icon/app.awaken.finance/50"},
        new WebsiteInfoDto { website = "beangotown.xyz", logo = "https://icon.horse/icon/beangotown.com/50"},
        new WebsiteInfoDto { website = "schrodingernft.ai",logo = "https://icon.horse/icon/cat.schrodingerai.com/50"},
        new WebsiteInfoDto { website = "beangotown.com" ,logo = "https://icon.horse/icon/beangotown.com/50"},
    };
}