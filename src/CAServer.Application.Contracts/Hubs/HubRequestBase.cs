namespace CAServer.Hubs;

public class HubRequestBase
{
    public HubRequestContext Context { get; set; }
}

public class HubRequestContext
{
    public string ClientId { get; set; }
    public string RequestId { get; set; }

    public override string ToString()
    {
        return $"ClientId={ClientId}, RequestId={RequestId}";
    }
}