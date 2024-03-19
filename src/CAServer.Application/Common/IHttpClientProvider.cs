using System.Collections.Generic;
using System.Threading.Tasks;

namespace CAServer.Common;

public interface IHttpClientProvider
{
    Task<T> GetAsync<T>(string url, Dictionary<string, string> headers = null, Dictionary<string, string> parameters = null,
        int timeout = 0);

    Task<T> PostAsync<T>(string url, object paramObj, Dictionary<string, string> headers);
}