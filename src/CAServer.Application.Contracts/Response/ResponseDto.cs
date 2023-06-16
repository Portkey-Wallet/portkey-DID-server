using System;

namespace CAServer.Response;

[Serializable]
public class ResponseDto
{
    public string Code { get; set; }
    public object Data { get; set; }
    public string Message { get; set; } = string.Empty;
}