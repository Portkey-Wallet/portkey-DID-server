using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.Search;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace CAServer.Controllers
{
    [RemoteService]
    [Area("app")]
    [ControllerName("Search")]
    [Route("api/app/search")]
    public class SearchController : CAServerController
    {
        private readonly ISearchAppService _searchAppService;

        public SearchController(ISearchAppService searchAppService)
        {
            _searchAppService = searchAppService;
        }

        [HttpGet("{indexName}")]
        public async Task<string> GetList(GetListInput input, string indexName)
        {
            return await _searchAppService.GetListByLucenceAsync(indexName, input);
        }
    }
}