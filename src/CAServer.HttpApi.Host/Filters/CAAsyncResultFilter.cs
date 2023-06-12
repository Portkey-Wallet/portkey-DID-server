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
            var result = context.Result;
            context.Result = new ResponseDto()
            {
                Data = context.Result
            };
        }

        await next();
    }
}