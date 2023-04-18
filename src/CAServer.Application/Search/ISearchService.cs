using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Indexing.Elasticsearch.Options;
using CAServer.Entities.Es;
using Microsoft.Extensions.Options;
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
        
        var (totalCount, items) = await _nestRepository.GetListByLucenceAsync(input.Filter, input.Sort, input.SortType,
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