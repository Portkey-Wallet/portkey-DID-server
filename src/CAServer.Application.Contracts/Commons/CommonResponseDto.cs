namespace CAServer.Commons;

public class CommonResponseDto<T>
{
    public string Code { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }

    public bool Success() => Code == CommonConstant.SuccessCode;
}