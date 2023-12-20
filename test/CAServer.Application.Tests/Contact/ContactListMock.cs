using System.Collections.Generic;
using CAServer.Options;
using Microsoft.Extensions.Options;
using Moq;

namespace CAServer.Contact;

public class ContactListMock
{
    public static IOptions<VariablesOptions> GetMockVariablesOptions()
    {
        var mockOptions = new Mock<IOptions<VariablesOptions>>();
        mockOptions.Setup(o => o.Value).Returns(
            new VariablesOptions
            {
                ImageMap = new Dictionary<string, string>()
                {
                    ["aelf"] = "aelfImage",
                    ["eth"] = "ethImage"
                }
            });
        return mockOptions.Object;
    }
}