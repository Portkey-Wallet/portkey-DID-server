namespace CAServer.ContactClean.Provider;

public class RelationOneResponseDto
{
    public string Code { get; set; }
    public string Desc { get; set; }
    public object Data { get; set; }
}

public class RelationOneResponseDto<T>
{
    public string Code { get; set; }
    public string Desc { get; set; }
    public T Data { get; set; }
}