using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Indexing.Elasticsearch.Options;
using CAServer.Entities.Es;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;

namespace CAServer.Search;

public interface ISearchService
{
    string IndexName { get; }

    Task<string> GetListByLucenceAsync(string indexName, GetListInput input);
}

public abstract class SearchService<TEntity, TKey> : ISearchService 
    where TEntity : class, IEntity<TKey>, new()
{
    public abstract string IndexName { get; }

    private readonly INESTRepository<TEntity, TKey> _nestRepository;

    protected SearchService(INESTRepository<TEntity, TKey> nestRepository)
    {
        _nestRepository = nestRepository;
    }

    public async Task<string> GetListByLucenceAsync(string indexName, GetListInput input)
    {
        Func<SortDescriptor<TEntity>, IPromise<IList<ISort>>> sort = null;
        if (!string.IsNullOrEmpty(input.Sort))
        {
            var sortList = ConvertSortOrder(input.Sort);
            var sortDescriptor = new SortDescriptor<TEntity>();
            sortDescriptor = sortList.Aggregate(sortDescriptor,
                (current, sortType) => current.Field(new Field(sortType.SortField), sortType.SortOrder));
            sort = s => sortDescriptor;
        }

        var (totalCount, items) = await _nestRepository.GetListByLucenceAsync(input.Filter, sort,
            input.MaxResultCount,
            input.SkipCount, indexName);

        var serializeSetting = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        return JsonConvert.SerializeObject(new PagedResultDto<TEntity>
        {
            Items = items,
            TotalCount = totalCount
        }, Formatting.None, serializeSetting);
    }

    private static IEnumerable<SortType> ConvertSortOrder(string sort)
    {
        var sortList = new List<SortType>();
        foreach (var sortOrder in sort.Split(","))
        {
            var array = sortOrder.Split(" ");
            var order = SortOrder.Ascending;
            if (string.Equals(array.Last(), "asc", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(array.Last(), "ascending", StringComparison.OrdinalIgnoreCase))
            {
                order = SortOrder.Ascending;
            }
            else if (string.Equals(array.Last(), "desc", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(array.Last(), "descending", StringComparison.OrdinalIgnoreCase))
            {
                order = SortOrder.Descending;
            }

            sortList.Add(new SortType
            {
                SortField = array.First(),
                SortOrder = order
            });
        }

        return sortList;
    }
}

public class UserTokenSearchService : SearchService<UserTokenIndex, Guid>
{
    private readonly IndexSettingOptions _indexSettingOptions;

    public override string IndexName => $"{_indexSettingOptions.IndexPrefix.ToLower()}.usertokenindex";

    public UserTokenSearchService(INESTRepository<UserTokenIndex, Guid> nestRepository,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions) : base(nestRepository)
    {
        _indexSettingOptions = indexSettingOptions.Value;
    }
}

public class ContactSearchService : SearchService<ContactIndex, Guid>
{
    private readonly IndexSettingOptions _indexSettingOptions;
    public override string IndexName => $"{_indexSettingOptions.IndexPrefix.ToLower()}.contactindex";

    public ContactSearchService(INESTRepository<ContactIndex, Guid> nestRepository,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions) : base(nestRepository)
    {
        _indexSettingOptions = indexSettingOptions.Value;
    }
}

public class ChainsInfoSearchService : SearchService<ChainsInfoIndex, string>
{
    private readonly IndexSettingOptions _indexSettingOptions;
    public override string IndexName => $"{_indexSettingOptions.IndexPrefix.ToLower()}.chainsinfoindex";

    public ChainsInfoSearchService(INESTRepository<ChainsInfoIndex, string> nestRepository,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions) : base(nestRepository)
    {
        _indexSettingOptions = indexSettingOptions.Value;
    }
}

public class AccountRecoverySearchService : SearchService<AccountRecoverIndex, Guid>
{
    private readonly IndexSettingOptions _indexSettingOptions;
    public override string IndexName => $"{_indexSettingOptions.IndexPrefix.ToLower()}.accountrecoverindex";

    public AccountRecoverySearchService(INESTRepository<AccountRecoverIndex, Guid> nestRepository,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions) : base(nestRepository)
    {
        _indexSettingOptions = indexSettingOptions.Value;
    }
}

public class AccountRegisterSearchService : SearchService<AccountRegisterIndex, Guid>
{
    private readonly IndexSettingOptions _indexSettingOptions;
    public override string IndexName => $"{_indexSettingOptions.IndexPrefix.ToLower()}.accountregisterindex";

    public AccountRegisterSearchService(INESTRepository<AccountRegisterIndex, Guid> nestRepository,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions) : base(nestRepository)
    {
        _indexSettingOptions = indexSettingOptions.Value;
    }
}

public class CAHolderSearchService : SearchService<CAHolderIndex, Guid>
{
    private readonly IndexSettingOptions _indexSettingOptions;
    public override string IndexName => $"{_indexSettingOptions.IndexPrefix.ToLower()}.caholderindex";

    public CAHolderSearchService(INESTRepository<CAHolderIndex, Guid> nestRepository,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions) : base(nestRepository)
    {
        _indexSettingOptions = indexSettingOptions.Value;
    }
}