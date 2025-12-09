using System.Collections.Generic;

namespace CAServer.Growth.Dtos;

public class BeInvitedConfigResponseDto
{
    public string ActivityTitle { get; set; }
    public Dictionary<string, BeInvitedConfigDto> Data { get; set; }
}

public class BeInvitedConfigDto
{
    public string MobileTitleCopyWriting { get; set; }

    public string PCTitleCopyWriting { get; set; }

    public string Logo { get; set; }

    public string CopyWriting { get; set; }
    public List<TaskConfig> TaskConfigs { get; set; }
    public Notice Notice { get; set; }
}

public class TaskConfig
{
    public string TaskNo { get; set; }

    public string TaskName { get; set; }

    public string Topic { get; set; }

    public string TaskCopyWriting { get; set; }

    public string ButtonName { get; set; }

    public bool ButtonAbled { get; set; }
    
    public string ButtonLink { get; set; }
}

public class Notice
{
    public string CopyWriting { get; set; }

    public string ButtonName { get; set; }
    
    public bool ButtonAbled { get; set; }
    
    public string ButtonLink { get; set; }

    public string NoticeName { get; set; }
}