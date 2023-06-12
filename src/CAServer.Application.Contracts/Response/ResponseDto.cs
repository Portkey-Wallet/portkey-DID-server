using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CAServer.Response;

[Serializable]
public class ResponseDto : ActionResult
{
    public string Code { get; set; } = "20000";
    public object Data { get; set; }
    public string Message { get; set; } = string.Empty;
}