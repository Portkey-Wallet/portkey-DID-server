namespace CAServer.Hubs;

public class HubResponse<T>
{
    public string RequestId { get; set; }
    public T Body { get; set; }
}