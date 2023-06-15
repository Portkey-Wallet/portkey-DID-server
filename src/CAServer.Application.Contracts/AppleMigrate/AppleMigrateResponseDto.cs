namespace CAServer.AppleMigrate;

public class AppleMigrateResponseDto
{
    public string Id { get; set; }
    public string Identifier { get; set; }
    public string IdentifierHash { get; set; }
    public string OriginalIdentifier { get; set; }
    public string Salt { get; set; }
}