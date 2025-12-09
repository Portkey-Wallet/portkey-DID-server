using System.Collections.Generic;
using CAServer.Commons.Etos;

namespace CAServer.UserAssets.Dtos;

public class GetRecentTransactionUsersDto
{
    public List<RecentTransactionUser> Data { get; set; }
    public long TotalRecordCount { get; set; }
}

public class RecentTransactionUser : ChainDisplayNameDto
{
    public string CaAddress { get; set; }
    public string Address { get; set; }
    public string AddressChainId { get; set; }
    public string TransactionTime { get; set; }
    public string Index { get; set; }
    public string Name { get; set; }
    public string Avatar { get; set; }
    public List<UserContactAddressDto> Addresses { get; set; }
}

public class UserContactAddressDto : ChainDisplayNameDto
{
    public string Address { get; set; }
    public string TransactionTime { get; set; }
}