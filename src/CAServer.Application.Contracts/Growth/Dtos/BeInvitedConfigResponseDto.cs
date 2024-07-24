using System.Collections.Generic;

namespace CAServer.Growth.Dtos;

public class BeInvitedConfigResponseDto
{
    public string TitleCopyWriting { get; set; }

    public string Logo { get; set; }

    public string CopyWriting { get; set; }

    public List<TaskConfig> TaskConfigs { get; set; }

    public Notice Notice { get; set; }

}

public class TaskConfig
{
    public int TaskNo { get; set; }

    public string Topic { get; set; }
    
    public string TaskCopyWriting { get; set; }

    public string ButtonName { get; set; }

    public bool ButtonShow { get; set; }
}

public class Notice
{
    public string CopyWriting { get; set; }

    public string ButtonName { get; set; }

    public bool ButtonShow { get; set; }
}