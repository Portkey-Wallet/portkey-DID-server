namespace CAServer.CAAccount.Dtos;

public class CAHolderExistsResponseDto
{
    public Success Success { get; set; }

    public Failed Failed { get; set; }
}

public class Success
{
    public Data Data { get; set; }
}

public class Failed
{
    public Error Error { get; set; }
    
    public Data Data { get; set; }
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