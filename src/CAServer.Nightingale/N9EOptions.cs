namespace CAServer.Nightingale;

public class N9EOptions
{
    public IList<Type> Clients { get; set; } = new List<Type>();
}

public static class N9ETelemetryConsumerOptionsExtensions
{
    public static N9EOptions AddClients<T>(this N9EOptions options)
        where T : IN9EClient
    {
        options.Clients.Add(typeof(T));
        return options;
    }
}