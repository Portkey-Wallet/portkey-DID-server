using System;
using JetBrains.Annotations;
using Volo.Abp;

namespace CAServer.Commons;

public class CommonResponseDto<T>
{
    private const string SuccessCode = "20000";
    private const string CommonErrorCode = "50000";

    
    public string Code { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }

    
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
    
    public CommonResponseDto<T> Error(string message)
    {
        Code = CommonErrorCode;
        Message = message;
        return this;
    }
    public CommonResponseDto<T> Error(string code, string message)
    {
        Code = code;
        Message = message;
        return this;
    }
    
    public CommonResponseDto<T> Error(Exception e, [CanBeNull] string message = null, [CanBeNull] string code = null)
    {
        return e is UserFriendlyException ufe
            ? Error(code ?? ufe.Code, message ?? ufe.Message)
            : Error(code ?? CommonErrorCode, message ?? e.Message);
    }



}