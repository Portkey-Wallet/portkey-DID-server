namespace CAServer.CAAccount.Dtos;

public class CAHolderExistsResponseDto
{
    public Data Data { get; set; }

    public Error Error { get; set; }
}


public class Error
{
    public int Code { get; set; }

    public string Message { get; set; }
}

public class Data
{
    public bool Result { get; set; }
}