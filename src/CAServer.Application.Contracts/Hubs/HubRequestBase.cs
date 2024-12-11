using Orleans;

namespace CAServer.Hubs;

[GenerateSerializer]
public class HubRequestBase
{
    [Id(0)]
    public HubRequestContext Context { get; set; }
}

[GenerateSerializer]
public class HubRequestContext
{
    [Id(0)]
    public string ClientId { get; set; }

    [Id(1)]
    public string RequestId { get; set; }

    public override string ToString()
    {
        return $"ClientId={ClientId}, RequestId={RequestId}";
    }
}