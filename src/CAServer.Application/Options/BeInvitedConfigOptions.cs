using System.Collections.Generic;

namespace CAServer.Options;

public class BeInvitedConfigOptions
{
    public string ActivityTitle { get; set; }
    public Dictionary<string, BeInvitedConfig> BeInvitedConfig { get; set; }
}

public class BeInvitedConfig
{
    public string MobileTitleCopyWriting { get; set; }

    public string PCTitleCopyWriting { get; set; }

    public string Logo { get; set; }

    public string CopyWriting { get; set; }

    public List<TaskConfigInfo> TaskConfigInfos { get; set; }

    public NoticeInfo NoticeInfo { get; set; }
}

public class TaskConfigInfo
{
    public string TaskNo { get; set; }
    public string Topic { get; set; }
    public string TaskName { get; set; }

    public string TaskCopyWriting { get; set; }

    public string ButtonName { get; set; }
    
    public bool ButtonAbled { get; set; }

    public string ButtonLink { get; set; }

}

public class NoticeInfo
{
    public string CopyWriting { get; set; }

    public string ButtonName { get; set; }
    
    public bool ButtonAbled { get; set; }
    
    public string ButtonLink { get; set; }
    
    public string NoticeName { get; set; }

}