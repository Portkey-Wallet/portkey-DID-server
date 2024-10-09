namespace CAServer.Grains.State;

public class GuardianState
{
    public string Id { get; set; }
    public string Identifier { get; set; }
    public string OriginalIdentifier { get; set; }
    public string IdentifierHash { get; set; }
    public string Salt { get; set; }
    public bool IsDeleted { get; set; }
    
    public string IdentifierPoseidonHash { get; set; }
}