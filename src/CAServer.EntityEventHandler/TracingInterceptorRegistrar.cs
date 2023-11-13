using System;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DynamicProxy;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.EntityEventHandler;

public class TracingInterceptorRegistrar
{
    public static void RegisterIfNeeded(IOnServiceRegistredContext context)
    {
        if (ShouldIntercept(context.ImplementationType))
        {
            context.Interceptors.TryAdd<TracingInterceptor>();
        }
    }

    private static bool ShouldIntercept(Type type)
    {
        if (DynamicProxyIgnoreTypes.Contains(type))
        {
            return false;
        }

        if (type == typeof(IDistributedEventHandler<>))
        {
            Console.WriteLine("###");
            return true;
        }

        return false;
    }
}