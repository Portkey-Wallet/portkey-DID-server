using CAServer.Options;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace CAServer.Phone;

public partial class PhoneInfoServiceTests
{
    private IOptions<PhoneInfoOptions> GetPhoneInfoOptions()
    {
        var phoneInfoItem = new PhoneInfoItem
        {
                Country = "12345678901234567890123456789012",
                Code = "sssss",
                Iso = "123"
        };
        var phoneInfoOptions = new PhoneInfoOptions();
        var phoneInfo = new List<PhoneInfoItem>();
        phoneInfo.Add(phoneInfoItem);
        phoneInfoOptions.PhoneInfo = phoneInfo;
        return new OptionsWrapper<PhoneInfoOptions>(
            phoneInfoOptions);
    }
}

