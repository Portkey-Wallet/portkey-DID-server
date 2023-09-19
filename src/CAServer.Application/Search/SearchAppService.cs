using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch.Options;
using CAServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization;
using Volo.Abp.Users;

namespace CAServer.Search;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class SearchAppService : CAServerAppService, ISearchAppService
{
    private readonly IEnumerable<ISearchService> _esServices;
    private readonly IndexSettingOptions _indexSettingOptions;
    private readonly EsIndexBlacklistOptions _esIndexBlacklistOptions;

    public SearchAppService(IEnumerable<ISearchService> esServices,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions,
        IOptionsSnapshot<EsIndexBlacklistOptions> esIndexBlacklistOptions)
    {
        _esServices = esServices;
        _indexSettingOptions = indexSettingOptions.Value;
        _esIndexBlacklistOptions = esIndexBlacklistOptions.Value;
    }

    public async Task<string> GetListByLucenceAsync(string indexName, GetListInput input)
    {
        try
        {
            CheckBlacklist(indexName);
            var indexPrefix = _indexSettingOptions.IndexPrefix.ToLower();
            var index = $"{indexPrefix}.{indexName}";
            
            var esService = _esServices.FirstOrDefault(e => e.IndexName == index);
            if (input.MaxResultCount > 1000)
            {
                input.MaxResultCount = 1000;
            }

            if (index.Equals($"{indexPrefix}.usertokenindex") ||
                index.Equals($"{indexPrefix}.contactindex") ||
                index.Equals($"{indexPrefix}.caholderindex"))
            {
                input.Filter = await CheckUserPermission(input.Filter);
            }

            return esService == null ? null : await esService.GetListByLucenceAsync(index, input);
        }
        catch (Exception e)
        {
            Logger.LogError("Search from es error.", e);
            throw;
        }
    }

    private Task<string> CheckUserPermission(string filter)
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new AbpAuthorizationException("Unauthorized.");
        }

        var userId = CurrentUser.GetId();
        filter = filter.IsNullOrEmpty() ? $"userId:{userId.ToString()}" : $"{filter} && userId:{userId.ToString()}";
        return Task.FromResult(filter);
    }

    private void CheckBlacklist(string indexName)
    {
        if (_esIndexBlacklistOptions.Indexes.Contains(indexName))
        {
            throw new UserFriendlyException("Not allowed.");
        }
    }
}