namespace CAServer.Response;

public static class ResponseDtoExtension
{
    private static readonly string Prefix = ResponseCode.ProjectId + ResponseCode.ServiceId;

    public static ResponseDto ObjectResult(this ResponseDto responseDto, object data)
    {
        responseDto.Code = Prefix + ResponseCode.ObjectResult;
        responseDto.Data = data;
        return responseDto;
    }

    public static ResponseDto NoContent(this ResponseDto responseDto)
    {
        responseDto.Code = Prefix + ResponseCode.NoContent;
        responseDto.Message = ResponseMessage.NoContent;
        return responseDto;
    }

    public static ResponseDto EmptyResult(this ResponseDto responseDto)
    {
        responseDto.Code = Prefix + ResponseCode.EmptyResult;
        responseDto.Message = ResponseMessage.EmptyResult;
        return responseDto;
    }

    public static ResponseDto UnhandedExceptionResult(this ResponseDto responseDto, string code, string message)
    {
        responseDto.Code = Prefix + code;
        responseDto.Message = message;
        return responseDto;
    }
}