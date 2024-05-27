using System;
using System.Collections.Generic;
using System.Linq;
using CAServer.Commons;
using CAServer.UserAssets.Dtos;
using CAServer.UserAssets.Provider;
using Newtonsoft.Json;
using Xunit;

namespace CAServer;


public class TempTest
{
    [Fact]
    public void Test1()
    {
        NftItem nftItem = new NftItem();
        nftItem.Traits = @"[{""traitType"":""background"",""value"":""red""},{""traitType"":""color"",""value"":""blue""}]";
        nftItem.CollectionSymbol = "SEED-50";
        CalculateAndSetTraitsPercentage(nftItem);
        string nftItemJson = JsonConvert.SerializeObject(nftItem);
        Console.WriteLine(nftItemJson);
    }
    
    private void CalculateAndSetTraitsPercentage(NftItem nftItem)
    {
        if (!string.IsNullOrEmpty(nftItem.Traits))
        {
            List<Trait> traitsList = JsonHelper.DeserializeJson<List<Trait>>(nftItem.Traits);
            if (traitsList == null || !traitsList.Any())
            {
                return;
            }
            
            Console.WriteLine("nftItem:" +  JsonConvert.SerializeObject(nftItem));
            List<Trait> allItemsTraitsList = GetAllTraitsInCollectionAsync(nftItem.CollectionSymbol);

            var traitTypeCounts = allItemsTraitsList.GroupBy(t => t.TraitType).ToDictionary(g => g.Key, g => g.Count());

            var traitTypeValueCounts = allItemsTraitsList.GroupBy(t => $"{t.TraitType}-{t.Value}")
                .ToDictionary(g => g.Key, g => g.Count());

            CalculateTraitsPercentages(nftItem, traitsList, traitTypeCounts, traitTypeValueCounts);
        }
    }
    
    private List<Trait> GetAllTraitsInCollectionAsync(string collectionSymbol)
    {
        var indexerNftInfos = new IndexerNftInfos();
        indexerNftInfos.CaHolderNFTBalanceInfo = new CaHolderNFTBalanceInfo();
        indexerNftInfos.CaHolderNFTBalanceInfo.Data = new List<IndexerNftInfo>();
        var indexerNftInfo = new IndexerNftInfo();
        indexerNftInfo.NftInfo = new NftInfo();
        indexerNftInfo.NftInfo.Symbol = "aa-1";
        indexerNftInfo.NftInfo.Supply = 1;
        indexerNftInfo.NftInfo.Traits = @"[{""traitType"":""background"",""value"":""red""},{""traitType"":""color"",""value"":""blue""}]";
        indexerNftInfos.CaHolderNFTBalanceInfo.Data.Add(indexerNftInfo);
        
        
        indexerNftInfo = new IndexerNftInfo();
        indexerNftInfo.NftInfo = new NftInfo();
        indexerNftInfo.NftInfo.Symbol = "aa-1";
        indexerNftInfo.NftInfo.Supply = 1;
        indexerNftInfo.NftInfo.Traits = @"[{""traitType"":""background"",""value"":""red""},{""traitType"":""color"",""value"":""blue""}]";

        indexerNftInfos.CaHolderNFTBalanceInfo.Data.Add(indexerNftInfo);
        
        indexerNftInfo = new IndexerNftInfo();
        indexerNftInfo.NftInfo = new NftInfo();
        indexerNftInfo.NftInfo.Symbol = "aa-2";
        indexerNftInfo.NftInfo.Supply = 1;
        indexerNftInfo.NftInfo.Traits = @"[{""traitType"":""background"",""value"":""blue""},{""traitType"":""color"",""value"":""blue""}]";
        indexerNftInfos.CaHolderNFTBalanceInfo.Data.Add(indexerNftInfo);
        
        indexerNftInfo = new IndexerNftInfo();
        indexerNftInfo.NftInfo = new NftInfo();
        indexerNftInfo.NftInfo.Symbol = "aa-2";
        indexerNftInfo.NftInfo.Supply = 1;
        indexerNftInfo.NftInfo.Traits = @"[{""traitType"":""background"",""value"":""blue""},{""traitType"":""color"",""value"":""blue""}]";
        indexerNftInfos.CaHolderNFTBalanceInfo.Data.Add(indexerNftInfo);
        
        indexerNftInfo = new IndexerNftInfo();
        indexerNftInfo.NftInfo = new NftInfo();
        indexerNftInfo.NftInfo.Symbol = "aa-3";
        indexerNftInfo.NftInfo.Supply = 1;
        indexerNftInfo.NftInfo.Traits = @"[{""traitType"":""background"",""value"":""blue""},{""traitType"":""color"",""value"":""red""}]";
        indexerNftInfos.CaHolderNFTBalanceInfo.Data.Add(indexerNftInfo);

        Console.WriteLine("indexerNftInfos:" +  JsonConvert.SerializeObject(indexerNftInfos));
        
        List<string> allItemsTraitsListInCollection = indexerNftInfos.CaHolderNFTBalanceInfo.Data != null && indexerNftInfos.CaHolderNFTBalanceInfo.Data.Any()
            ? indexerNftInfos.CaHolderNFTBalanceInfo.Data
                .Where(nftInfo => nftInfo.NftInfo != null && nftInfo.NftInfo.Supply > 0)
                .GroupBy(nftInfo => nftInfo.NftInfo.Symbol)
                .Select(group => group.First().NftInfo.Traits)
                .ToList()
            : new List<string>();
        
        Console.WriteLine("allItemsTraitsListInCollection:" + JsonConvert.SerializeObject(allItemsTraitsListInCollection));

        List<Trait> allItemsTraitsList = allItemsTraitsListInCollection
            .Select(traits => JsonHelper.DeserializeJson<List<Trait>>(traits))
            .Where(curTraitsList => curTraitsList != null && curTraitsList.Any())
            .SelectMany(curTraitsList => curTraitsList)
            .ToList();
        
        Console.WriteLine("allItemsTraitsList:" + JsonConvert.SerializeObject(allItemsTraitsList));

        return allItemsTraitsList;
    }
    
        
    private void CalculateTraitsPercentages(NftItem nftItem, List<Trait> traitsList, Dictionary<string, int> traitTypeCounts,
        Dictionary<string, int> traitTypeValueCounts)
    {
        foreach (var trait in traitsList)
        {
            string traitType = trait.TraitType;
            string traitTypeValue = $"{trait.TraitType}-{trait.Value}";
            
            if (traitTypeCounts.ContainsKey(traitType) && traitTypeValueCounts.ContainsKey(traitTypeValue))
            {
                int numerator = traitTypeValueCounts[traitTypeValue];
                int denominator = traitTypeCounts[traitType];
                string percentage = PercentageHelper.CalculatePercentage(numerator, denominator);
                trait.Percent = percentage;
            } else {
                trait.Percent = "-";
            }
        }
        
        Console.WriteLine("traitsList:" + JsonConvert.SerializeObject(traitsList));

    }
    
}