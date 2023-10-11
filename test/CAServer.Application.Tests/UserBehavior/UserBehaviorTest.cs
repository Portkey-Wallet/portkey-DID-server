using System;
using Xunit;

namespace CAServer.UserBehavior;

public class UserBehaviorTest : CAServerApplicationTestBase
{
    public void ParseRefererTest()
    {
        var referer = "https://www.beangotown.com/login?wd=123";
        if (string.IsNullOrWhiteSpace(referer))
        {
            return ;
        }

        var uri = new Uri(referer);
        var host = uri.Host;
        var index = host.IndexOf(".", StringComparison.Ordinal);
        if (index == -1)
        {
            return ;
        }

        var hostSub =  host.Substring(0, index);
        Assert.Equal("www", hostSub);
    }
}