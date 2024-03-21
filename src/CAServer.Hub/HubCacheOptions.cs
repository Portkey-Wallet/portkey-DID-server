namespace CAServer.Hub;

public class HubCacheOptions
{
    public Dictionary<string, int> MethodResponseTtl { get; set; }
    public int DefaultResponseTtl { get; set; }
    public RouteTableConfig RouteTableConfig { get; set; }
}

public class RouteTableConfig
{
    public string LocalIp { get; set; }
    public int Port { get; set; }
}