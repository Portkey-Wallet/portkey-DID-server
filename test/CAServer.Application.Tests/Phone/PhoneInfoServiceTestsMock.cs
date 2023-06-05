using CAServer.Options;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using CAServer.IpInfo;
using Moq;

namespace CAServer.Phone;

public partial class PhoneInfoServiceTests
{
    private IOptions<PhoneInfoOptions> GetPhoneInfoOptions()
    {
        var phoneInfoOptions = new PhoneInfoOptions();
        var phoneInfo = new List<PhoneInfoItem>();
        phoneInfo.Add(new PhoneInfoItem
        {
            Country = "Singapore",
            Code = "65",
            Iso = "SG"
        });
        phoneInfo.Add(new PhoneInfoItem()
        {
            
            Country = "United States",
            Code = "1",
            Iso = "US"
        });
        phoneInfoOptions.PhoneInfo = phoneInfo;
        phoneInfoOptions.Default = new PhoneInfoItem
        {
            Country = "Singapore",
            Code = "65",
            Iso = "SG"
        };
        return new OptionsWrapper<PhoneInfoOptions>(
            phoneInfoOptions);
    }
    
    private IIpInfoClient GetIpInfoClient()
    {

        IpInfoDto nowhere = new IpInfoDto
        {
            CountryName = "MockCountry",
            CountryCode = "NOT_FOUND",
            Location = new LocationInfo
            {
                CallingCode = "404"
            }
        };
        
        IpInfoDto us = new IpInfoDto
        {
            CountryName = "United States",
            CountryCode = "US",
            Location = new LocationInfo
            {
                CallingCode = "1"
            }
        };

        var ipInfoClient = new Mock<IIpInfoClient>();
        ipInfoClient.Setup(m => m.GetIpInfoAsync(It.Is<string>(ip => ip == "0.0.0.0"))).ReturnsAsync(nowhere);
        ipInfoClient.Setup(m => m.GetIpInfoAsync(It.Is<string>(ip => ip == "20.230.34.112"))).ReturnsAsync(us);

        return ipInfoClient.Object;
    }
}

