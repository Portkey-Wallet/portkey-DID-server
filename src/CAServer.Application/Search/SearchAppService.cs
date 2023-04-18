using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch.Options;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Search;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class SearchAppService : CAServerAppService, ISearchAppService
{
    private readonly IEnumerable<ISearchService> _esServices;
    private readonly IndexSettingOptions _indexSettingOptions;

    public SearchAppService(IEnumerable<ISearchService> esServices,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions)
    {
        _esServices = esServices;
        _indexSettingOptions = indexSettingOptions.Value;
    }
    
    public async Task<string> GetListByLucenceAsync(string indexName, GetListInput input)
    {
        try
        {
            var indexPrefix = _indexSettingOptions.IndexPrefix.ToLower();
            var index = $"{indexPrefix}.{indexName}";
            var esService = _esServices.FirstOrDefault(e => e.IndexName == index);
            if (input.MaxResultCount > 1000)
            {
                input.MaxResultCount = 1000;
            }

            // if (index.Equals($"{indexPrefix}.usertokenindex") || index.Equals($"{indexPrefix}.contactindex"))
            // {
            //     input.Filter = await CheckUserPermission(input.Filter);
            // }

            return esService == null ? null : await esService.GetListByLucenceAsync(index, input);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    // private Task<string> CheckUserPermission(string filter)
    // {
    //     if (!CurrentUser.Id.HasValue)
    //     {
    //         throw new AbpAuthorizationException("Unauthorized.");
    //     }
    //
    //     var userId = CurrentUser.GetId();
    //     filter = filter.IsNullOrEmpty() ? $"userId:{userId.ToString()}" : $"{filter} && userId:{userId.ToString()}";
    //     return Task.FromResult(filter);
    // }
}