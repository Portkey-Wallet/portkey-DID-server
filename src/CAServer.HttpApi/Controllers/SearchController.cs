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
        private readonly ITestOrleansAppService _testOrleansAppService;

        public SearchController(ISearchAppService searchAppService, ITestOrleansAppService testOrleansAppService)
        {
            _searchAppService = searchAppService;
            _testOrleansAppService = testOrleansAppService;
        }

        [HttpGet("{indexName}")]
        public async Task<string> GetList(GetListInput input, string indexName)
        {
            return await _searchAppService.GetListByLucenceAsync(indexName, input);
        }
        
        [HttpGet("test-orleans")]
        public async Task<string> TestOrleans(string grainName, string grainKey)
        {
            return await _testOrleansAppService.TestOrleansAsync(grainName, grainKey);
        }
    }
}