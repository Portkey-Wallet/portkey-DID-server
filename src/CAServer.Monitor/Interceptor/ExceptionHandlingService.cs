using System.Text;
using AElf.ExceptionHandler;
using Newtonsoft.Json;
using Serilog;

namespace CAServer.Monitor.Interceptor;

public class ExceptionHandlingService
{
    public static async Task<FlowBehavior> HandleExceptionP0(Exception ex)
    {
        Log.Error(HandleException("HandleExceptionP0", ex));
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }
    public static async Task<FlowBehavior> HandleExceptionP1(Exception ex, object p1)
    {
        Log.Error(HandleException("HandleExceptionP1", ex, p1));
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }

    public static async Task<FlowBehavior> HandleExceptionP2(Exception ex, object p1, object p2)
    {
        Log.Error(HandleException("HandleExceptionP2", ex, p1, p2));
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }

    public static async Task<FlowBehavior> HandleExceptionP3(Exception ex, object p1, object p2, object p3)
    {
        Log.Error(HandleException("HandleExceptionP3", ex, p1, p2, p3));
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }

    public static string HandleException(string methodName, Exception ex, params object[] args)
    {
        var result = new StringBuilder($"{methodName}\targs:");
        if (args == null || args.Length == 0)
        {
            return result.AppendFormat(" null\t").AppendFormat(GetFullStackTrace(ex)).ToString();
        }

        for (var i = 0; i < args.Length; i++)
        {
            result.AppendFormat("{0}-{1};", i,JsonConvert.SerializeObject(args[i]));
        }

        return result.AppendFormat(GetFullStackTrace(ex)).ToString();
    }

    public static string GetFullStackTrace(Exception ex)
    {
        if (ex == null)
        {
            throw new ArgumentNullException(nameof(ex));
        }

        StringBuilder sb = new StringBuilder($"\tException:");
        while (ex != null)
        {
            sb.Append(ex.Message.Replace(Environment.NewLine, " ").Replace("\n", " ").Replace("\r", " "));
            sb.Append(ex.StackTrace.Replace(Environment.NewLine, " ").Replace("\n", " ").Replace("\r", " "));
            ex = ex.InnerException;
        }

        return sb.ToString();
    }
}