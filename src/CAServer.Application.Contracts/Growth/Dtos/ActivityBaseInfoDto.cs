using System.Collections.Generic;

namespace CAServer.Growth.Dtos;

public class ActivityBaseInfoDto
{

    public List<ActivityBaseInfo> Data { get; set; }

}

public class ActivityBaseInfo
{
    
    public string ActivityName { get; set; }

    public int ActivityValue { get; set; }

    public bool IsDefault { get; set; }

    public string StartDate { get; set; }
    
    public string EndDate { get; set; }

    public string DateRange { get; set; }
    
}