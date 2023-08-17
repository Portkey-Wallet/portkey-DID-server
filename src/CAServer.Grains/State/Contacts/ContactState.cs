using CAServer.Grains.Grain.Contacts;

namespace CAServer.Grains.State.Contacts;

public class ContactState
{
    public Guid Id { get; set; }
    public string Index { get; set; }
    public string Name { get; set; }
    public string Avatar { get; set; }
    public List<ContactAddress> Addresses { get; set; } = new();
    public Guid UserId { get; set; }
    public CaHolderInfo CaHolderInfo { get; set; }
    public ImInfo ImInfo { get; set; }
    public bool IsDeleted { get; set; } = true;
    public bool IsImputation { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime ModificationTime { get; set; }
}

public class CaHolderInfo
{
    public Guid UserId { get; set; }
    public string CaHash { get; set; }
    public string WalletName { get; set; }
}

public class ImInfo
{
    public string RelationId { get; set; }
    public string PortkeyId { get; set; }
    public string Name { get; set; }
}