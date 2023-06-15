namespace CAServer.Options;

public class SendVerifierCodeRequestLimitOptions
{
    public int Limit { get; set; }
    
    public int ExpireHours { get; set; }

}