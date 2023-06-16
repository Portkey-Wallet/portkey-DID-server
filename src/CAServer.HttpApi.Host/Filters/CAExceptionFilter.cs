using System;
using System.Text;
using System.Threading.Tasks;
using CAServer.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.ExceptionHandling;
using Volo.Abp.Authorization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ExceptionHandling;
using Volo.Abp.Http;
using Volo.Abp.Json;

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
        var exceptionHandlingOptions = context.GetRequiredService<IOptions<AbpExceptionHandlingOptions>>().Value;
        var exceptionToErrorInfoConverter = context.GetRequiredService<IExceptionToErrorInfoConverter>();
        var remoteServiceErrorInfo = exceptionToErrorInfoConverter.Convert(context.Exception, options =>
        {
            options.SendExceptionsDetailsToClients = exceptionHandlingOptions.SendExceptionsDetailsToClients;
            options.SendStackTraceToClients = exceptionHandlingOptions.SendStackTraceToClients;
        });

        var logLevel = context.Exception.GetLogLevel();

        var remoteServiceErrorInfoBuilder = new StringBuilder();
        remoteServiceErrorInfoBuilder.AppendLine($"---------- {nameof(RemoteServiceErrorInfo)} ----------");
        remoteServiceErrorInfoBuilder.AppendLine(context.GetRequiredService<IJsonSerializer>()
            .Serialize(remoteServiceErrorInfo, indented: true));

        var logger = context.GetService<ILogger<AbpExceptionFilter>>(NullLogger<AbpExceptionFilter>.Instance);

        logger.LogWithLevel(logLevel, remoteServiceErrorInfoBuilder.ToString());

        logger.LogException(context.Exception, logLevel);

        await context.GetRequiredService<IExceptionNotifier>()
            .NotifyAsync(new ExceptionNotificationContext(context.Exception));

        if (context.Exception is AbpAuthorizationException)
        {
            await context.HttpContext.RequestServices.GetRequiredService<IAbpAuthorizationExceptionHandler>()
                .HandleAsync(context.Exception.As<AbpAuthorizationException>(), context.HttpContext);
        }
        else
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;

            var code = "50000";
            if (context.Exception is IHasErrorCode hasErrorCodeException &&
                !string.IsNullOrWhiteSpace(hasErrorCodeException.Code))
            {
                code = hasErrorCodeException.Code;
            }

            context.Result =
                new ObjectResult(
                    new ResponseDto().UnhandedExceptionResult(code, context.Exception.Message));
        }

        context.Exception = null; //Handled!
    }
}