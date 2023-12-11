using System.Collections.Generic;
using System.Threading.Tasks;

namespace CAServer.Common;

public interface IHttpClientProvider
{
    Task<T> GetAsync<T>(string url, Dictionary<string, string> headers);
    Task<T> PostAsync<T>(string url, object paramObj, Dictionary<string, string> headers);
}