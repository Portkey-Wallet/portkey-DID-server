namespace CAServer.Transfer.Dtos;

public class ResponseWrapDto<T>
{
    public string Code { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }
}