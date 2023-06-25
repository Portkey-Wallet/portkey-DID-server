using System.Threading.Tasks;
using CAServer.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CAServer.Filters;

public class CaAsyncResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is EmptyResult or NoContentResult)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        }

        if (context.Result is ObjectResult objectResult)
        {
            if (objectResult?.Value is not ResponseDto)
            {
                context.Result = new ObjectResult(new ResponseDto().ObjectResult(objectResult?.Value));
            }
        }
        else if (context.Result is EmptyResult)
        {
            context.Result = new ObjectResult(new ResponseDto().EmptyResult());
        }
        else if (context.Result is NoContentResult)
        {
            context.Result = new ObjectResult(new ResponseDto().NoContent());
        }
        else if (context.Result is StatusCodeResult)
        {
            context.Result =
                new ObjectResult(new ResponseDto().StatusCodeResult(context.HttpContext.Response.StatusCode,
                    context.Result.GetType().Name));
            
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        }
        
        await next();
    }
}