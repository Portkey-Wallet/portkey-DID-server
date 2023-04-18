using System.Collections.Generic;

namespace CAServer.UserAssets.Dtos;

public class GetRecentTransactionUsersDto
{
    public List<RecentTransactionUser> Data { get; set; }
}

public class RecentTransactionUser
{
    public string ChainId { get; set; }
    public string CaAddress { get; set; }
    public string TransactionTime { get; set; }
    public string Name { get; set; }
}