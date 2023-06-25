using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Response;

public static class ResponseDtoExtension
{
    public static ResponseDto ObjectResult(this ResponseDto responseDto, object data)
    {
        responseDto.Code = ResponseCode.ObjectResult;
        responseDto.Data = data;
        return responseDto;
    }

    public static ResponseDto NoContent(this ResponseDto responseDto)
    {
        responseDto.Code = ResponseCode.NoContent;
        responseDto.Message = ResponseMessage.NoContent;
        return responseDto;
    }

    public static ResponseDto EmptyResult(this ResponseDto responseDto)
    {
        responseDto.Code = ResponseCode.EmptyResult;
        responseDto.Message = ResponseMessage.EmptyResult;
        return responseDto;
    }

    public static ResponseDto UnhandedExceptionResult(this ResponseDto responseDto, string code, string message)
    {
        responseDto.Code = code;
        responseDto.Message = message;
        return responseDto;
    }

    public static ValidationResponseDto ValidationResult(this ValidationResponseDto responseDto,
        string message, IList<ValidationResult> validationErrors)
    {
        responseDto.Code = ResponseCode.ValidationError;
        responseDto.Message = message;
        responseDto.ValidationErrors = validationErrors;
        return responseDto;
    }
}