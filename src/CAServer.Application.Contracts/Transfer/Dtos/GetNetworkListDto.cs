using System.Collections.Generic;
using CAServer.Commons;
using CAServer.Commons.Etos;
using JetBrains.Annotations;

namespace CAServer.Transfer.Dtos;

public class GetNetworkListDto : ChainDisplayNameDto
{
    public List<NetworkDto> NetworkList { get; set; }
}

public class ReceiveNetworkDto
{
    public SortedDictionary<string, List<NetworkInfoDto>> DestinationMap { get; set; }
        = new SortedDictionary<string, List<NetworkInfoDto>>(new ChainComparer());
}

public class SendNetworkDto
{
    public List<NetworkInfoDto> NetworkList { get; set; }
}

public class NetworkInfoDto
{
    public string Network { get; set; }
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public List<ServiceDto> ServiceList { get; set; } // null if in aelf
}

public class ServiceDto
{
    public string ServiceName { get; set; }
    public string MultiConfirmTime { get; set; }
    public string MaxAmount { get; set; } // only with ETransfer
}

public class NetworkDto
{
    public string Network { get; set; }
    public string Name { get; set; }
    public string MultiConfirm { get; set; }
    public string MultiConfirmTime { get; set; }
    public string ContractAddress { get; set; }
    public string ExplorerUrl { get; set; }
    public string Status { get; set; }
    [CanBeNull] public string WithdrawFee { get; set; }
    [CanBeNull] public string WithdrawFeeUnit { get; set; }
    public bool SpecialWithdrawFeeDisplay { get; set; }
    public string SpecialWithdrawFee { get; set; }
}
