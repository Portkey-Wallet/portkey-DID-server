namespace CAServer.Notify.Dtos;

public class PullNotifyResultDto
{
    public string Title { get; set; }
    public string Content { get; set; }
    public string TargetVersion { get; set; }
    public string DownloadUrl { get; set; }
    public bool IsForceUpdate { get; set; }
    public StyleType StyleType { get; set; }
}