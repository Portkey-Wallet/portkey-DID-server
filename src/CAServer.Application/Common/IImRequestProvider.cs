using System.Collections.Generic;
using System.Threading.Tasks;

namespace CAServer.Common;

public interface IImRequestProvider
{
    Task<T> GetAsync<T>(string url);
    Task<T> GetAsync<T>(string url, IDictionary<string, string> headers);
    Task<T> PostAsync<T>(string url);
    Task<T> PostAsync<T>(string url, object paramObj);
    Task<T> PostAsync<T>(string url, object paramObj, Dictionary<string, string> headers);
    Task<T> PostAsync<T>(string url, RequestMediaType requestMediaType, object paramObj,
            Dictionary<string, string> headers);
}