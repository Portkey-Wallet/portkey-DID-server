namespace SignatureServer.Dtos;

public class CommonResponse<T>
{
    public const string SuccessCode = "20000";
    public const string DefaultErrorCode = "50000";

    public string Code { get; set; } = SuccessCode;
    public string Message { get; set; } = "";
    public bool Success => Code == SuccessCode;
    public T? Data { get; set; }


    private CommonResponse()
    {
    }

    public CommonResponse(T data)
    {
        Data = data;
    }
    
    public static CommonResponse<T> Error(string message, string code = DefaultErrorCode)
    {
        return new CommonResponse<T>
        {
            Code = code,
            Message = message
        };
    }
    
}