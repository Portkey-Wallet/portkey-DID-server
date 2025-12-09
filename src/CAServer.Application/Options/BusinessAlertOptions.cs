namespace CAServer.Options;

public class BusinessAlertOptions
{
    public string Webhook { get; set; }
    /// <summary>
    /// seconds
    /// </summary>
    public int SendInterval { get; set; } = 3;
}