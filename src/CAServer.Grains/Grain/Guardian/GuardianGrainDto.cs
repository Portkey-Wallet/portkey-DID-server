namespace CAServer.Grains.Grain.Guardian;

[GenerateSerializer]
public class GuardianGrainDto
{
    [Id(0)]
    public string Id { get; set; }

    [Id(1)]
    public string Identifier { get; set; }

    [Id(2)]
    public string OriginalIdentifier { get; set; }

    [Id(3)]
    public string IdentifierHash { get; set; }

    [Id(4)]
    public string Salt { get; set; }

    [Id(5)]
    public bool IsDeleted { get; set; }

    [Id(6)]
    public string IdentifierPoseidonHash { get; set; }
}