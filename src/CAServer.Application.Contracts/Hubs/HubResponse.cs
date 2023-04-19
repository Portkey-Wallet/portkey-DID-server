namespace CAServer.Hubs;

public class HubResponse<T> : HubResponseBase<T>
{
    public string RequestId { get; set; }
}

public class HubResponseBase<T>
{
    public T Body { get; set; }
}