using System.Threading.Tasks;

namespace CAServer.Common;

public interface IHttpClientService
{
    Task<T> GetAsync<T>(string url);
}