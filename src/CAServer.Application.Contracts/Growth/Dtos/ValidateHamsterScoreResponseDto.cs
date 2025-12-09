namespace CAServer.Growth.Dtos;

public class ValidateHamsterScoreResponseDto
{
    public Result Result { get; set; }

    public ErrorMsg ErrorMsg { get; set; }
}


public class ErrorMsg
{
    public string Message { get; set; }
}

public class Result
{
    public bool ValidateResult { get; set; } = false;
}