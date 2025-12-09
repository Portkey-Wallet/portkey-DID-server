namespace CAServer.Verifier;

public class ResponseResultDto<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
}