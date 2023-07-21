using System.Collections.Generic;
using CAServer.Options;
using Microsoft.Extensions.Options;

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
}