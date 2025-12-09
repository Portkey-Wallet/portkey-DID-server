using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using CAServer.Commons;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace CAServer.Common;

public interface IMessagePushRequestProvider
{
    Task PostAsync(string url, object paramObj);
}

public class MessagePushRequestProvider : IMessagePushRequestProvider, ISingletonDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MessagePushRequestProvider> _logger;

    public MessagePushRequestProvider(IHttpClientFactory httpClientFactory, ILogger<MessagePushRequestProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task PostAsync(string url, object paramObj)
    {
        var requestInput = paramObj == null ? string.Empty : JsonConvert.SerializeObject(paramObj, Formatting.None);

        var requestContent = new StringContent(
            requestInput,
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = _httpClientFactory.CreateClient(MessagePushConstant.MessagePushServiceName);

        var response = await client.PostAsync(url, requestContent);
        if (!ResponseSuccess(response.StatusCode))
        {
            var content = await response.Content.ReadAsStringAsync();
            
            _logger.LogError(
                "message push service response not success, url:{url}, code:{code}, message: {message}, params:{param}",
                url, response.StatusCode, content, JsonConvert.SerializeObject(paramObj));

            throw new UserFriendlyException(content, ((int)response.StatusCode).ToString());
        }
    }

    private bool ResponseSuccess(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.OK or HttpStatusCode.NoContent;
}