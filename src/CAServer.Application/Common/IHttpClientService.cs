using System.Collections.Generic;
using System.Threading.Tasks;

namespace CAServer.Common;

public interface IHttpClientService
{
    Task<T> GetAsync<T>(string url);
    Task<T>  GetAsync<T>(string url, Dictionary<string, string> headers);
}