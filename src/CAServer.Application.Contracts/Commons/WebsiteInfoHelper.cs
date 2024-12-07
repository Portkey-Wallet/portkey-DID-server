using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CAServer.Contacts;

namespace CAServer.Commons;

public class WebsiteInfoHelper
{
    private static Dictionary<string, WebsiteInfoDto> CacheLogoMap = new Dictionary<string, WebsiteInfoDto>();
    private static bool _isInitialized = false;
    private static readonly object _lockObject = new object();

    public static async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            lock (_lockObject)
            {
                if (!_isInitialized)
                {
                    _isInitialized = true;
                }
                else
                {
                    return;
                }
            }

            foreach (var info in WebsiteInfoes)
            {
                await LogoProcessor.SaveLogo(info.Logo);
            }
        }
    }


    public static Task<bool> WebsiteAvailable(WebsiteInfoParamDto param)
    {
        InitializeAsync();

        SetWebsite(param);
        bool? cacheAvailable = IsCacheAvailable(param);
        if (null != cacheAvailable)
        {
            return Task.FromResult(cacheAvailable.Value);
        }

        return IsKeeperAvailable(param);
    }

    private static bool? IsCacheAvailable(WebsiteInfoParamDto param)
    {
        WebsiteInfoDto info;
        if (CacheLogoMap.TryGetValue(param.Logo, out info))
        {
            if (null == info)
            {
                return false;
            }

            return param.Website.Contains(info.Website) && IsSpenderAvailable(param.Spender, info.Spenders);
        }

        return null;
    }

    private static async Task<bool> IsKeeperAvailable(WebsiteInfoParamDto param)
    {
        foreach (var info in WebsiteInfoes)
        {
            if (info.Logo.Equals(param.Logo))
            {
                return info.Website.Equals(param.Website) && IsSpenderAvailable(param.Spender, info.Spenders);
            }
        }

        bool saveResult = await LogoProcessor.SaveLogo(param.Logo);
        if (!saveResult)
        {
            return false;
        }

        foreach (var info in WebsiteInfoes)
        {
            var same = LogoProcessor.CalculateGrayImageSimilarity(GetLogoUrlMd5(info.Logo), GetLogoUrlMd5(param.Logo));
            Console.WriteLine($"IsKeeperAvailable {same} - {param.Logo} - {info.Logo}");
            if (same > 0.95)
            {
                CacheLogoMap.Add(param.Logo, info);
                return param.Website.Contains(info.Website) && IsSpenderAvailable(param.Spender, info.Spenders);
            }
        }

        CacheLogoMap.Add(param.Logo, null);
        return true;
    }

    private static bool IsSpenderAvailable(string spender, List<string> spenders)
    {
        if (null != spenders && spenders.Count > 0)
        {
            return spenders.Contains(spender);
        }

        return true;
    }

    public static string GetLogoUrlMd5(string logoUrl)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(logoUrl);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString() + ".jpg";
        }
    }

    private static void SetWebsite(WebsiteInfoParamDto param)
    {
        //sb
        if (param.Website.StartsWith("https://testv2.beangotown.com/") || 
            param.Website.StartsWith("https://test.beangotown.com/") ||
            param.Website.StartsWith("https://beangotown.com/"))
        {
            param.Website = "beangotown.com";
            return;
        }

        foreach (var pixiepoint in Pixiepoints)
        {
            if (param.Website.Contains(pixiepoint.Key))
            {
                foreach (var websiteInfoDto in WebsiteInfoes)
                {
                    if (websiteInfoDto.Website.Equals(pixiepoint.Value))
                    {
                        param.Website = websiteInfoDto.Website;
                    }
                }
            }
        }
    }

    private static List<WebsiteInfoDto> WebsiteInfoes = new List<WebsiteInfoDto>
    {
        new WebsiteInfoDto
        {
            Website = "etransfer.exchange",
            Logo = "https://icon.horse/icon/app.etransfer.exchange/50",
            Spenders = new List<string>
            {
                "2w13DqbuuiadvaSY2ZyKi2UoXg354zfHLM3kwRKKy85cViw4ZF", "x4CTSuM8typUbpdfxRZDTqYVa42RdxrwwPkXX7WUJHeRmzE6k",
                "4xWFvoLvi5anZERDuJvzfMoZsb6WZLATEzqzCVe8sQnCp2XGS", "2AgU8BfyKyrxUrmskVCUukw63Wk96MVfVoJzDDbwKszafioCN1"
            }
        },
        new WebsiteInfoDto
        {
            Website = "ebridge.exchange",
            Logo = "https://icon.horse/icon/ebridge.exchange/50",
            Spenders = new List<string>
            {
                "2dKF3svqDXrYtA5mYwKfADiHajo37mLZHPHVVuGbEDoD9jSgE8", "GZs6wyPDfz3vdEmgVd3FyrQfaWSXo9uRvc7Fbp5KSLKwMAANd",
                "foDLAM2Up3xLjg43SvCy5Ed6zaY5CKG8uczj6yUVZUweqQUmz", "JKjoabe2wyrdP1P8TvNyD4GZP6z1PuMvU2y5yJ4JTeBjTMAoX"
            }
        },
        new WebsiteInfoDto
        {
            Website = "awaken.finance",
            Logo = "https://icon.horse/icon/app.awaken.finance/50",
            Spenders = new List<string>
            {
                "JKjoabe2wyrdP1P8TvNyD4GZP6z1PuMvU2y5yJ4JTeBjTMAoX", "JvDB3rguLJtpFsovre8udJeXJLhsV1EPScGz2u1FFneahjBQm",
                "83ju3fGGnvQzCmtjApUTwvBpuLQLQvt5biNMv4FXCvWKdZgJf", "2q7NLAr6eqF4CTsnNeXnBZ9k4XcmiUeM61CLWYaym6WsUmbg1k",
                "UYdd84gLMsVdHrgkr3ogqe1ukhKwen8oj32Ks4J1dg6KH9PYC", "BEakVbMWHXqQAn3oj3nj2dPk8jfFeJeTg9C99rPZiYTBhGB1a",
                "T3mdFC35CQSatUXQ5bQ886pULo2TnzS9rfXxmsoZSGnTq2a2S",
                "2YnkipJ9mty5r6tpTWQAwnomeeKUT7qCWLHKaSeV1fejYEyCdX", "fGa81UPViGsVvTM13zuAAwk1QHovL3oSqTrCznitS4hAawPpk",
                "LzkrbEK2zweeuE4P8Y23BMiFY2oiKMWyHuy5hBBbF1pAPD2hh", "EG73zzQqC8JencoFEgCtrEUvMBS2zT22xoRse72XkyhuuhyTC",
                "23dh2s1mXnswi4yNW7eWNKWy7iac8KrXJYitECgUctgfwjeZwP", "2vahJs5WeWVJruzd1DuTAu3TwK8jktpJ2NNeALJJWEbPQCUW4Y",
                "2BC4BosozC1x27izqrSFJ51gYYtyVByjKGZvmitY7EBFDDPYHN"
            }
        },
        new WebsiteInfoDto { Website = "aefinder.io", Logo = "https://icon.horse/icon/aefinder.io/50" },
        new WebsiteInfoDto { Website = "schrodingernft.ai", Logo = "https://icon.horse/icon/cat.schrodingernft.ai/50" },
        new WebsiteInfoDto
        {
            Website = "eforest.finance",
            Logo = "https://icon.horse/icon/www.eforest.finance/50",
            Spenders = new List<string>
            {
                "2cGT3RZZy6UJJ3eJPZdWMmuoH2TZBihvMtAtKvLJUaBnvskK2x", "iupiTuL2cshxB9UNauXNXe9iyCcqka7jCotodcEHGpNXeLzqG",
                "ZYNkxNAzswRC8UeHc6bYMdRmbmLqYDPqZv7sE5d9WuJ5rRQEi", "mhgUyGhd27YaoG8wgXTbwtbAiYx7E59n5GXEkmkTFKKQTvGnB",
                "XmQ59e3JxmtP5gGafNFyJQAF5A2WbtVDYXFVv3JEaKMckyb3b",
                "zv7YnQ2dLM45ssfifN1dpwqBwdxH13pqGm9GDH6peRdH8F3hD", "SRVEHfZoiifcHYfnTagJvtW3QtkGnVo1rEEssKk8hirHX8xed",
                "gjGmHom31GWr5VPWf11de3mJGHVdaDFsR4zgrqjrbijYXv6TW", "1EFmvua5WQiv15N3xF4egEUvkvLGNWHdoYLMcbXdaXxzrGmA",
                "yEMwBeheRq6iiw6VN9TgUt2eASBNcgxsEUUwFFsgXedySgnp2"
            }
        },
        new WebsiteInfoDto { Website = "pixiepoints.io", Logo = "https://icon.horse/icon/pixiepoints.io/50" },
        new WebsiteInfoDto { Website = "tmrwdao.com", Logo = "https://icon.horse/icon/tmrwdao.com/50" },
        new WebsiteInfoDto
        {
            Website = "ewell.finance",
            Logo = "https://icon.horse/icon/ewell.finance/50",
            Spenders = new List<string>
            {
                "2WCTWEdrVgqgJyoYbuVEDn2RQrfiptUtMUtKvP7TiZp94gUgwJ", "2EbbUpZLds58keVZPJDLPRbPpxzUYCcjooq6LBiBoRXVTFZTiQ"
            }
        },
        new WebsiteInfoDto
        {
            Website = "hamster.beangotown",
            Logo = "https://icon.horse/icon/hamster.beangotown.com/50",
            Spenders = new List<string>
            {
                "2Mt11RFsR9TEt1kDpod6yPPTNjKMcb5vuuoRwJ4A8VCV7GuBzi", "m39bMdjpA74Pv7pyA4zn8w6mhz182KpcrtFAnwWCiFmcihNYE"
            }
        },
        new WebsiteInfoDto
        {
            Website = "beangotown.com",
            Logo = "https://icon.horse/icon/beangotown.com/50",
            Spenders = new List<string>
            {
                "oZHKLeudXJpZeKi55hA5KHgyv7eWBwPL4nCiCChNqPBc6Hb3F", "C7ZUPUHDwG2q3jR5Mw38YoBHch2XiZdiK6pBYkdhXdGrYcXsb"
            }
        },
    };


    private static Dictionary<string, string> Pixiepoints = new Dictionary<string, string>
    {
        { "schrodingerai.com", "schrodingernft.ai" },
        { "awakenswap.xyz", "awaken.finance" },
        { "schrodingernft.ai", "schrodingernft.ai" },
        { "beangotown.xyz", "hamster.beangotown" },
        { "beangotown.com", "hamster.beangotown" },
    };
}