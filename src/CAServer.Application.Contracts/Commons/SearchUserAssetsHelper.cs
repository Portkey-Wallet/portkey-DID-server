using System.Collections.Generic;
using System.Linq;
using CAServer.UserAssets.Dtos;
using Volo.Abp.ObjectMapping;

namespace CAServer.Commons;

public class SearchUserAssetsHelper
{
    public static SearchUserAssetsV2Dto ToSearchV2(SearchUserAssetsDto dto, List<NftCollection> fromCollectionList, IObjectMapper objectMapper)
    {
        SearchUserAssetsV2Dto result = new SearchUserAssetsV2Dto
        {
            TotalRecordCount = dto.TotalRecordCount
        };

        if (dto?.Data?.Count == 0)
        {
            return result;
        }

        result.TokenInfos = dto.Data.Where(p => p.TokenInfo != null)
            .Select(p => ToTokenInfoV2(p, objectMapper)).ToList();
        result.NftInfos = ToCollection(dto.Data, fromCollectionList);

        return result;
    }

    public static TokenInfoV2Dto ToTokenInfoV2(UserAsset dto, IObjectMapper objectMapper)
    {
        var result = objectMapper.Map<TokenInfoDto, TokenInfoV2Dto>(dto.TokenInfo);
        result.ChainId = dto.ChainId;
        result.Symbol = dto.Symbol;
        result.Address = dto.Address;
        ChainDisplayNameHelper.SetDisplayName(result);
        return result;
    }

    public static List<NftCollectionDto> ToCollection(List<UserAsset> assets, List<NftCollection> fromCollectionList)
    {
        var result = new List<NftCollectionDto>();
        if (fromCollectionList?.Count == 0)
        {
            return result;
        }

        var nftMap = assets.Where(p => p.NftInfo != null).Select(p =>
        {
            p.NftInfo.ChainId = p.ChainId;
            return p.NftInfo;
        }).GroupBy(p => p.CollectionName);
        foreach (var nftEntry in nftMap)
        {
            result.Add(new NftCollectionDto
            {
                CollectionName = nftEntry.Key,
                Items = nftEntry.ToList()
            });
        }

        SetCollectionImage(result, fromCollectionList);

        return result;
    }

    private static void SetCollectionImage(List<NftCollectionDto> collectionList, List<NftCollection> fromCollectionList)
    {
        var collectionMap = fromCollectionList
            .GroupBy(p => p.CollectionName)
            .Select(group => group.First());
        foreach (var collection in collectionList)
        {
            collection.ImageUrl = collectionMap.FirstOrDefault(p => p.CollectionName == collection.CollectionName)?.ImageUrl;
        }
    }
}