namespace CAServer.Commons.Etos;

public class BaseEto<T>
{

    public BaseEto(T data)
    {
        Data = data;
    }
    
    public T Data { get; set; }
    
}