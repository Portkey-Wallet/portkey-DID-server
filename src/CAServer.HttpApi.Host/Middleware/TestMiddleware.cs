using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CAServer.Middleware;

public class TestMiddleware
{
    private readonly RequestDelegate _next;

    public TestMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {

        throw new Exception("test midd");

        await _next(context);
    }
}

public static class RequestCultureMiddlewareExtensions
{
    public static IApplicationBuilder UseTestResponse(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TestMiddleware>();
    }
}