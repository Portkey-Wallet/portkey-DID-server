using System;
using System.Threading.Tasks;
using CAServer.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Authorization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ExceptionHandling;
using Volo.Abp.Http;
using Volo.Abp.Validation;

namespace CAServer.Filters;

public class CAExceptionFilter : IAsyncExceptionFilter, ITransientDependency
{
    public virtual async Task OnExceptionAsync(ExceptionContext context)
    {
        if (!ShouldHandleException(context))
        {
            return;
        }

        await HandleAndWrapException(context);
    }

    protected virtual bool ShouldHandleException(ExceptionContext context)
    {
        if (context.ActionDescriptor.IsControllerAction() &&
            context.ActionDescriptor.HasObjectResult())
        {
            return true;
        }

        if (context.HttpContext.Request.CanAccept(MimeTypes.Application.Json))
        {
            return true;
        }

        if (context.HttpContext.Request.IsAjax())
        {
            return true;
        }

        return false;
    }

    protected virtual async Task HandleAndWrapException(ExceptionContext context)
    {
        var logLevel = context.Exception.GetLogLevel();
        var logger = context.GetService<ILogger<CAExceptionFilter>>(NullLogger<CAExceptionFilter>.Instance);
        
        logger.LogException(context.Exception, logLevel);

        if (context.Exception is AbpAuthorizationException)
        {
            await context.HttpContext.RequestServices.GetRequiredService<IAbpAuthorizationExceptionHandler>()
                .HandleAsync(context.Exception.As<AbpAuthorizationException>(), context.HttpContext);
        }
        else
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;

            var code = ResponseCode.UnhandledException;
            if (context.Exception is IHasErrorCode hasErrorCodeException &&
                !string.IsNullOrWhiteSpace(hasErrorCodeException.Code))
            {
                code = hasErrorCodeException.Code;
            }

            if (context.Exception is IHasValidationErrors { ValidationErrors.Count: > 0 } validationErrors)
            {
                context.Result =
                    new ObjectResult(
                        new ValidationResponseDto().ValidationResult(context.Exception.Message,
                            validationErrors.ValidationErrors));

                context.Exception = null;
                return;
            }

            context.Result =
                new ObjectResult(
                    new ResponseDto().UnhandedExceptionResult(code, context.Exception.Message));
        }

        context.Exception = null; //Handled!
    }
}