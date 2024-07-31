using System.Collections.Generic;

namespace CAServer.Options;



public class ActivityConfigOptions
{
    public Dictionary<string, ActivityConfigs> ActivityConfigMap { get; set; }

}

public class ActivityConfigs
{
    public string ActivityName { get; set; }

    public ActivityConfig ActivityConfig { get; set; }

    public RulesConfig RulesConfig { get; set; }

    public bool IsDefault { get; set; }
}

public class RulesConfig
{
    public bool IsRulesShow { get; set; }

    public string RulesDesc { get; set; }

    public string RulesUrl { get; set; }
}

public class ActivityConfig
{
    public string ActivityTitle { get; set; }

    public string CopyWriting { get; set; }

    public bool IsShow { get; set; }

    public string ImageUrl { get; set; }

    public string StartDate { get; set; }
    public string StartDateFormat { get; set; }

    public string EndDate { get; set; }
    public string EndDateFormat { get; set; }

    public string TaskImageUrl { get; set; }
    
    public string PCTaskImageUrl { get; set; }


}