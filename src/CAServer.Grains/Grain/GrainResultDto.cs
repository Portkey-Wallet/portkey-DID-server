namespace CAServer.Grains.Grain;

[GenerateSerializer]
public class GrainResultDto<T> : GrainResultDto
{
    [Id(0)]
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

[GenerateSerializer]
public class GrainResultDto
{
    [Id(0)]
    public bool Success { get; set; }
    [Id(1)]
    public string Message { get; set; } = string.Empty;
}