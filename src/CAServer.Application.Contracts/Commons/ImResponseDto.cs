namespace CAServer.Commons;

public class ImResponseDto<T>
{
    public string Code { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
}