using System.Threading.Tasks;

namespace CAServer.Search;

public interface ISearchAppService
{
    Task<string> GetListByLucenceAsync(string indexName,GetListInput input);
}