namespace CAServer.Growth.Dtos;

public class ActivityDetailsResponseDto
{
    public string ActivityName { get; set; }

    public ActivityConfigDto ActivityConfig { get; set; }

    public RulesConfigDto RulesConfig { get; set; }

    public bool IsDefault { get; set; }
}

public class RulesConfigDto
{
    public bool IsRulesShow { get; set; }

    public string RulesDesc { get; set; }
    
    public string RulesUrl { get; set; }
}

public class ActivityConfigDto
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