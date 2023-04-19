using System.Collections.Generic;
using CAServer.UserAssets.Provider;
using Xunit;

namespace CAServer.UserAssets;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class CAHolderManagerInfoTests : CAServerApplicationTestBase
{
    public CAHolderManagerInfoTests()
    {
    }

    [Fact]
    public async void CaHolderManagerInfoTest()
    {
        CAHolderManagerInfo cAHolderManagerInfo = new CAHolderManagerInfo();
        cAHolderManagerInfo.CaHolderManagerInfo = new List<CAHolderManager>();
        var holderManagerInfo = cAHolderManagerInfo.CaHolderManagerInfo;
        
        ManagerHolder managerHolder = new ManagerHolder();
        managerHolder.Manager = "4";
        managerHolder.DeviceString = "5";
        string managerHolderManager = managerHolder.Manager;
        string managerHolderDeviceString = managerHolder.DeviceString;
        var managerHolders = new List<ManagerHolder>();
        managerHolders.Add(managerHolder);
            
        var caHolderManager = new CAHolderManager();
        caHolderManager.ChainId = "1";
        caHolderManager.CaHash = "2";
        caHolderManager.CaAddress = "3";
        caHolderManager.Managers = new List<ManagerHolder>();
        
        string caHolderManagerChainId = caHolderManager.ChainId;
        string caHolderManagerCaHash = caHolderManager.CaHash;
        string caHolderManagerCaAddress = caHolderManager.CaAddress;
        string caHolderManagerManagers = caHolderManager.Managers.ToString();
        holderManagerInfo.Add(caHolderManager);
    }
    
    [Fact]
    public async void NftCollectionInfoTest()
    {
        NftCollectionInfo nftCollectionInfo = new NftCollectionInfo();
        nftCollectionInfo.Symbol = "1";
        nftCollectionInfo.Decimals = 2;
        nftCollectionInfo.TokenName = "3";
        nftCollectionInfo.ImageUrl = "4";
        nftCollectionInfo.TotalSupply = 5;
        
        string nftCollectionInfoSymbol = nftCollectionInfo.Symbol;
        int nftCollectionInfoDecimals = nftCollectionInfo.Decimals;
        string nftCollectionInfoTokenName = nftCollectionInfo.TokenName;
        string nftCollectionInfoImageUrl = nftCollectionInfo.ImageUrl;
        long nftCollectionInfoTotalSupply = nftCollectionInfo.TotalSupply;

    }

}