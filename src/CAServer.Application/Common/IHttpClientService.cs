using System.Collections.Generic;
using System.Threading.Tasks;

namespace CAServer.Common;

public interface IHttpClientService
{
    Task<T> GetAsync<T>(string url);
    Task PostJsonAsync(string url, object paramObj, Dictionary<string, string> headers);
}