using System.Collections.Generic;

namespace CAServer.RedPackage.Dtos;

public class RedPackageConfigOutput
{
    public List<RedPackageTokenInfo> TokenInfo { get; set; }
    public List<ContractAddressInfo> RedPackageContractAddress { get; set; }
}