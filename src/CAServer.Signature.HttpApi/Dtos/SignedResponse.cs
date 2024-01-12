namespace SignatureServer.Dtos;

public class SignedResponse<T>
{

    public string Code { get; set; } = "20000";
    public string Message { get; set; } = "";
    public bool Success => Code == "20000";
    public string? DataSignature { get; set; }
    public T? Data { get; set; }


    private SignedResponse()
    {
    }

    public SignedResponse(T data, string signature)
    {
        Data = data;
        DataSignature = signature;
    }
    
    public static SignedResponse<T> Error(string message, string code = "50000")
    {
        return new SignedResponse<T>
        {
            Code = code,
            Message = message
        };
    }
    
}