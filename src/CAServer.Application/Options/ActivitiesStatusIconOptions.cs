namespace CAServer.Options;

public class ActivitiesStatusIconOptions
{
    public string Receive { get; set; } =
        "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/activity/ReceiveIcon.svg";
    public string Send { get; set; } = "https://portkey-did.s3.ap-northeast-1.amazonaws.com/img/activity/SendIcon.svg";
}