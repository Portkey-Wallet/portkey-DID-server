using System;
using AutoResponseWrapper.Response;
using JetBrains.Annotations;
using Volo.Abp;

namespace CAServer.Commons;

public static class ResponseHelper
{
    private const string SuccessCode = "20000";
    private const string CommonErrorCode = "50000";

    public static ResponseDto Success([CanBeNull] this ResponseDto responseDto)
    {
        return responseDto.Success(null);
    }
    
    public static ResponseDto Success([CanBeNull] this ResponseDto responseDto, object data)
    {
        responseDto = responseDto ?? new ResponseDto();
        responseDto.Code = SuccessCode;
        responseDto.Data = data;
        return responseDto;
    }
    public static ResponseDto Error([CanBeNull] this ResponseDto responseDto, string code, string message)
    {
        responseDto = responseDto ?? new ResponseDto();
        responseDto.Code = code;
        responseDto.Message = message;
        return responseDto;
    }
    

    public static ResponseDto Error([CanBeNull] this ResponseDto responseDto, Exception e, string message = null)
    {
        return e is UserFriendlyException ufe
            ? responseDto.Error(ufe.Code, message ?? ufe.Message)
            : responseDto.Error(CommonErrorCode, message ?? e.Message);
    }


}