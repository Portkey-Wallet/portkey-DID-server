using AElf.Indexing.Elasticsearch.Options;
using CAServer.Search;
using Microsoft.Extensions.Options;

namespace CAServer.EntityEventHandler.Tests.Token;

public class MockSearchAppService : ISearchAppService
{
    private readonly IEnumerable<ISearchService> _esServices;
    private readonly IndexSettingOptions _indexSettingOptions;

    public MockSearchAppService(IEnumerable<ISearchService> esServices,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions)
    {
        _esServices = esServices;
        _indexSettingOptions = indexSettingOptions.Value;
    }
    public async Task<string> GetListByLucenceAsync(string indexName, GetListInput input)
    {
        var indexPrefix = _indexSettingOptions.IndexPrefix.ToLower();
        var index = $"{indexPrefix}.{indexName}";
        var esService = _esServices.FirstOrDefault(e => e.IndexName == index);
        if (input.MaxResultCount > 1000)
        {
            input.MaxResultCount = 1000;
        }

        return esService == null ? null : await esService.GetListByLucenceAsync(index, input);
    }
}