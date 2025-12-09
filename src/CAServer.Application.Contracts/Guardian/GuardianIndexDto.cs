using System;

namespace CAServer.Guardian;

public class GuardianIndexDto
{
    public string Id { get; set; }
    public string Identifier { get; set; }
    public string IdentifierHash { get; set; }
    public string OriginalIdentifier { get; set; }
    public string Salt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
    
    public string IdentifierPoseidonHash { get; set; }
    public string CaHash { get; set; }
    public string SecondaryEmail { get; set; }
}