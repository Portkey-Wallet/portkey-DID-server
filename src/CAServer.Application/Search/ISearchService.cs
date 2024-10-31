using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Indexing.Elasticsearch.Options;
using CAServer.Chain;
using CAServer.Entities.Es;
using CAServer.Search.Dtos;
using CAServer.UserAssets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.ObjectMapping;

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

    protected readonly INESTRepository<TEntity, TKey> _nestRepository;

    protected SearchService(INESTRepository<TEntity, TKey> nestRepository)
    {
        _nestRepository = nestRepository;
    }

    public virtual async Task<string> GetListByLucenceAsync(string indexName, GetListInput input)
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

    protected static IEnumerable<SortType> ConvertSortOrder(string sort)
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
    private readonly IObjectMapper _objectMapper;
    private readonly IAssetsLibraryProvider _assetsLibraryProvider;

    public override string IndexName => $"{_indexSettingOptions.IndexPrefix.ToLower()}.usertokenindex";

    public UserTokenSearchService(INESTRepository<UserTokenIndex, Guid> nestRepository,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions, IObjectMapper objectMapper,
        IOptionsSnapshot<IndexSettingOptions> optionsSnapshot, IAssetsLibraryProvider assetsLibraryProvider) : base(nestRepository)
    {
        _indexSettingOptions = indexSettingOptions.Value;
        _objectMapper = objectMapper;
        _indexSettingOptions = optionsSnapshot.Value;
        _assetsLibraryProvider = assetsLibraryProvider;
    }
    
    public override async Task<string> GetListByLucenceAsync(string indexName, GetListInput input)
    {
        Func<SortDescriptor<UserTokenIndex>, IPromise<IList<ISort>>> sort = null;
        if (!string.IsNullOrEmpty(input.Sort))
        {
            var sortList = ConvertSortOrder(input.Sort);
            var sortDescriptor = new SortDescriptor<UserTokenIndex>();
            sortDescriptor = sortList.Aggregate(sortDescriptor,
                (current, sortType) => current.Field(new Field(sortType.SortField), sortType.SortOrder));
            sort = s => sortDescriptor;
        }

        var (totalCount, items) = await _nestRepository.GetListByLucenceAsync(input.Filter, sort,
            input.MaxResultCount,
            input.SkipCount, indexName);
        
        //If the queried index is "usertokenindex", add Token ImageUrl information.
        List<UserTokenIndexDto> userTokenIndexDtos = null;
        if (items != null)
        {
            userTokenIndexDtos = new List<UserTokenIndexDto>();
            foreach (UserTokenIndex item in items)
            {
                var userTokenIndexDto =
                    _objectMapper.Map<UserTokenIndex, UserTokenIndexDto>(item);
                if (userTokenIndexDto.Token != null)
                {
                    userTokenIndexDto.Token.ImageUrl =
                        _assetsLibraryProvider.buildSymbolImageUrl(userTokenIndexDto.Token.Symbol);
                }

                userTokenIndexDtos.Add(userTokenIndexDto);
            }
        }

        var serializeSetting = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        return JsonConvert.SerializeObject(new PagedResultDto<UserTokenIndexDto>
        {
            Items = userTokenIndexDtos,
            TotalCount = totalCount
        }, Formatting.None, serializeSetting);
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
    private readonly ILogger<ChainsInfoSearchService> _logger;
    private readonly IObjectMapper _objectMapper;
    public override string IndexName => $"{_indexSettingOptions.IndexPrefix.ToLower()}.chainsinfoindex";

    public ChainsInfoSearchService(INESTRepository<ChainsInfoIndex, string> nestRepository,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions,ILogger<ChainsInfoSearchService> logger,IObjectMapper objectMapper) : base(nestRepository)
    {
        _indexSettingOptions = indexSettingOptions.Value;
        _logger = logger;
        _objectMapper = objectMapper;
    }

    public override async Task<string> GetListByLucenceAsync(string indexName, GetListInput input)
    {
        _logger.LogInformation($"GetListByLucenceAsync chainsinfoindex start: {indexName}");
        Func<SortDescriptor<ChainsInfoIndex>, IPromise<IList<ISort>>> sort = null;
        if (!string.IsNullOrEmpty(input.Sort))
        {
            var sortList = ConvertSortOrder(input.Sort);
            var sortDescriptor = new SortDescriptor<ChainsInfoIndex>();
            sortDescriptor = sortList.Aggregate(sortDescriptor,
                (current, sortType) => current.Field(new Field(sortType.SortField), sortType.SortOrder));
            sort = s => sortDescriptor;
        }

        var (totalCount, items) = await _nestRepository.GetListByLucenceAsync(input.Filter, sort,
            input.MaxResultCount,
            input.SkipCount, indexName);
        
        List<ChainResultDto> chainResultDtos = new List<ChainResultDto>();
        foreach (var entity in items)
        {
            chainResultDtos.Add(_objectMapper.Map<ChainsInfoIndex, ChainResultDto>(entity));
        }
        
        var serializeSetting = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        return JsonConvert.SerializeObject(new PagedResultDto<ChainResultDto>
        {
            Items = chainResultDtos,
            TotalCount = totalCount
        }, Formatting.None, serializeSetting);
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
public class OrderSearchService : SearchService<RampOrderIndex, Guid>
{
    private readonly IndexSettingOptions _indexSettingOptions;
    public override string IndexName => $"{_indexSettingOptions.IndexPrefix.ToLower()}.ramporderindex";

    public OrderSearchService(INESTRepository<RampOrderIndex, Guid> nestRepository,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions) : base(nestRepository)
    {
        _indexSettingOptions = indexSettingOptions.Value;
    }
}
public class UserExtraInfoSearchService : SearchService<UserExtraInfoIndex, String>
{
    private readonly IndexSettingOptions _indexSettingOptions;
    public override string IndexName => $"{_indexSettingOptions.IndexPrefix.ToLower()}.userextrainfoindex";

    public UserExtraInfoSearchService(INESTRepository<UserExtraInfoIndex, String> nestRepository,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions) : base(nestRepository)
    {
        _indexSettingOptions = indexSettingOptions.Value;
    }
}

public class NotifySearchService : SearchService<NotifyRulesIndex, Guid>
{
    private readonly IndexSettingOptions _indexSettingOptions;
    public override string IndexName => $"{_indexSettingOptions.IndexPrefix.ToLower()}.notifyrulesindex";

    public NotifySearchService(INESTRepository<NotifyRulesIndex, Guid> nestRepository,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions) : base(nestRepository)
    {
        _indexSettingOptions = indexSettingOptions.Value;
    }
}

public class GuardianSearchService : SearchService<GuardianIndex, String>
{
    private readonly IndexSettingOptions _indexSettingOptions;
    public override string IndexName => $"{_indexSettingOptions.IndexPrefix.ToLower()}.guardianindex";

    public GuardianSearchService(INESTRepository<GuardianIndex, String> nestRepository,
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

public class GrowthSearchService : SearchService<GrowthIndex, string>
{
    private readonly IndexSettingOptions _indexSettingOptions;
    public override string IndexName => $"{_indexSettingOptions.IndexPrefix.ToLower()}.growthindex";

    public GrowthSearchService(INESTRepository<GrowthIndex, string> nestRepository,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions) : base(nestRepository)
    {
        _indexSettingOptions = indexSettingOptions.Value;
    }
}

public class AccelerateRegisterSearchService : SearchService<AccelerateRegisterIndex, string>
{
    private readonly IndexSettingOptions _indexSettingOptions;
    public override string IndexName => $"{_indexSettingOptions.IndexPrefix.ToLower()}.accelerateregisterindex";

    public AccelerateRegisterSearchService(INESTRepository<AccelerateRegisterIndex, string> nestRepository,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions) : base(nestRepository)
    {
        _indexSettingOptions = indexSettingOptions.Value;
    }
}

public class AccelerateRecoverySearchService : SearchService<AccelerateRecoverIndex, string>
{
    private readonly IndexSettingOptions _indexSettingOptions;
    public override string IndexName => $"{_indexSettingOptions.IndexPrefix.ToLower()}.acceleraterecoverindex";

    public AccelerateRecoverySearchService(INESTRepository<AccelerateRecoverIndex, string> nestRepository,
        IOptionsSnapshot<IndexSettingOptions> indexSettingOptions) : base(nestRepository)
    {
        _indexSettingOptions = indexSettingOptions.Value;
    }
}