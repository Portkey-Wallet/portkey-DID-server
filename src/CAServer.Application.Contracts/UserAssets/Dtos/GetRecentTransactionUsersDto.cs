using System.Collections.Generic;

namespace CAServer.UserAssets.Dtos;

public class GetRecentTransactionUsersDto
{
    public List<RecentTransactionUser> Data { get; set; }
    public long TotalRecordCount { get; set; }
}

public class RecentTransactionUser
{
    public string ChainId { get; set; }
    public string CaAddress { get; set; }
    public string Address { get; set; }
    public string AddressChainId { get; set; }
    public string TransactionTime { get; set; }
    public string Index { get; set; }
    public string Name { get; set; }
    public List<UserContactAddressDto> Addresses { get; set; }
}

public class UserContactAddressDto
{
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string TransactionTime { get; set; }
}