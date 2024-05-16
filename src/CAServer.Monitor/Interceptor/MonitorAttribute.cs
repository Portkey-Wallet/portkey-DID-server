namespace CAServer.Monitor.Interceptor;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class MonitorAttribute : Attribute
{
    public MonitorAttribute()
    {
        
    }
}