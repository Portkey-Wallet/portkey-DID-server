using System;

namespace CAServer.Contacts;

public class CaHolderInfoDto
{
    public Guid UserId { get; set; }
    public string CaHash { get; set; }
    public string WalletName { get; set; }
}