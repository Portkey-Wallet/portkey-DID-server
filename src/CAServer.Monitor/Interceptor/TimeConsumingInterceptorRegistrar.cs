using System;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DynamicProxy;

namespace CAServer.Monitor.Interceptor;

// OnRegistred
public class TimeConsumingInterceptorRegistrar
{
    public static void RegisterIfNeeded(IOnServiceRegistredContext context)
    {
        if (ShouldIntercept(context.ImplementationType))
        {
            context.Interceptors.TryAdd<TimeConsumingInterceptor>();
        }
    }

    private static bool ShouldIntercept(Type type)
    {
        if (DynamicProxyIgnoreTypes.Contains(type))
        {
            return false;
        }

        // add other condition
        // if (type == typeof(GraphQLHelper))
        // {
        //     return true;
        // }

        return false;
    }
}