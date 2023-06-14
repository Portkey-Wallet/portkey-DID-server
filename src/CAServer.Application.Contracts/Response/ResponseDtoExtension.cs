namespace CAServer.Response;

public static class ResponseDtoExtension
{
    public static ResponseDto ObjectResult(this ResponseDto responseDto, object data)
    {
        responseDto.Data = data;
        return responseDto;
    }

    public static ResponseDto NoContent(this ResponseDto responseDto)
    {
        responseDto.Code = "000000";
        responseDto.Message = "no content";

        return responseDto;
    }

    public static ResponseDto EmptyResult(this ResponseDto responseDto)
    {
        responseDto.Code = "000000";
        responseDto.Message = "no content";

        return responseDto;
    }
}