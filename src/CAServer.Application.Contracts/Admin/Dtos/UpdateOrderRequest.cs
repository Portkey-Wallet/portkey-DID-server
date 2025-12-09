
namespace CAServer.Admin.Dtos;

public class MfaRequest<T>
{
    public string GoogleTfaPin { get; set; }

    public string Reason { get; set; }
    
    public T Data { get; set; }
}    