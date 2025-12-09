namespace CAServer.Guardian;

public class GuardianEto
{
    public string Id { get; set; }
    public string Identifier { get; set; }
    public string OriginalIdentifier { get; set; }
    public string IdentifierHash { get; set; }
    public string Salt { get; set; }
    public string IdentifierPoseidonHash { get; set; }
    
    public string CaHash { get; set; }
    public string SecondaryEmail { get; set; }
}