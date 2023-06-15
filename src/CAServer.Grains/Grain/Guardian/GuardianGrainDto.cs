namespace CAServer.Grains.Grain.Guardian;

public class GuardianGrainDto
{
    public string Id { get; set; }
    public string Identifier { get; set; }
    public string IdentifierHash { get; set; }
    public string OriginalIdentifier { get; set; }
    public string Salt { get; set; }
}