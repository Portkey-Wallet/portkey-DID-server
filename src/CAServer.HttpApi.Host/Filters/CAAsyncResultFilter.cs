using System.Threading.Tasks;
using CAServer.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CAServer.Filters;

public class CaAsyncResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is not ResponseDto)
        {
            if (context.Result is ObjectResult)
            {
                var objectResult = context.Result as ObjectResult;
                context.Result = new ObjectResult(new ResponseDto
                {
                    Data = objectResult.Value
                });
            }
        }

        await next();
    }
}