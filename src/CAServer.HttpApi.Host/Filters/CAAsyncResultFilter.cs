using System.Threading.Tasks;
using CAServer.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CAServer.Filters;

public class CaAsyncResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
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

        await next();
    }
}