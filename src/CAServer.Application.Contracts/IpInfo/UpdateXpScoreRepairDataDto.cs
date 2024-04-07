namespace CAServer.IpInfo;

public class UpdateXpScoreRepairDataDto
{
    public string UserId { get; set; }
    public decimal RawScore { get; set; }
    public decimal ActualScore { get; set; }
}

public class XpScoreRepairDataDto
{
    public string UserId { get; set; }
    public int Score { get; set; }
}