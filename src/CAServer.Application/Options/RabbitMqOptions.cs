using Volo.Abp.RabbitMQ;

namespace CAServer.Options;

public class RabbitMqOptions : AbpRabbitMqOptions
{

    public string ClientId { get; set; } = "Default";

}