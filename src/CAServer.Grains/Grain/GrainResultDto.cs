namespace CAServer.Grains.Grain;

public class GrainResultDto<T> : GrainResultDto
{
    public T Data { get; set; }


    public GrainResultDto()
    {
    }
    
    public GrainResultDto(T data)
    {
        Success = true;
        Data = data;
    }

    public GrainResultDto<T> Error(string message)
    {
        base.Success = false;
        Message = message;
        return this;
    }
    
}

public class GrainResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}