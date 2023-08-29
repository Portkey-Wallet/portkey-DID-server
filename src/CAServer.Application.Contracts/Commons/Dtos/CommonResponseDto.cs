using System;
using AutoResponseWrapper.Response;
using JetBrains.Annotations;
using Volo.Abp;

namespace CAServer.Commons.Dtos;

public class CommonResponseDto<T> : ResponseDto
{
    private const string SuccessCode = "20000";
    private const string CommonErrorCode = "50000";

    public bool Success => Code == SuccessCode;


    public CommonResponseDto()
    {
        Code = SuccessCode; 
    }
    
    public CommonResponseDto(T data)
    {
        Code = SuccessCode;
        Data = data;
    }
    
    public CommonResponseDto<T> Error(string code, string message)
    {
        Code = code;
        Message = message;
        return this;
    }
    
    public CommonResponseDto<T> Error(Exception e, [CanBeNull] string message = null)
    {
        return e is UserFriendlyException ufe
            ? Error(ufe.Code, message ?? ufe.Message)
            : Error(CommonErrorCode, message ?? e.Message);
    }



}