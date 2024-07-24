using System.Collections.Generic;

namespace CAServer.Options;

public class BeInvitedConfigOptions
{

    public Dictionary<string, BeInvitedConfig> BeInvitedConfig { get; set; }

}

public class BeInvitedConfig
{
    public string TitleCopyWriting { get; set; }

    public string Logo { get; set; }

    public string CopyWriting { get; set; }

    public List<TaskConfigInfo> TaskConfigInfos { get; set; }

    public NoticeInfo NoticeInfo { get; set; }

}

public class TaskConfigInfo
{
    public int TaskNo { get; set; }
    public string Topic { get; set; }
    
    public string TaskCopyWriting { get; set; }

    public string ButtonName { get; set; }

    public bool ButtonShow { get; set; }
}

public class NoticeInfo
{
    public string CopyWriting { get; set; }

    public string ButtonName { get; set; }

    public bool ButtonShow { get; set; }
}