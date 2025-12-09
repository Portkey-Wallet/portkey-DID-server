namespace CAServer.Message;

public class MessageBase<T>
{
    public string TargetClientId { get; set; }
    public T Message { get; set; }
}