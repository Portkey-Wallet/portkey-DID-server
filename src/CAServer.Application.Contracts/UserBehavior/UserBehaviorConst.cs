using System.Collections.Generic;

namespace CAServer.UserBehavior;

public class UserBehaviorConst
{
    public const string Referer = "Referer";
    public const string UserAgent = "User-Agent";
    public const string Origin = "Origin";
    public const string Unknown = "unknown";

    public static Dictionary<string, string> HostMapping = new Dictionary<string, string>()
    {
        { "www.beangotown.com", "beangotown" }
    };
    
}