namespace CAServer.Guardian;

public class GuardianIndexerInfoDto
{
    public string ChainId { get; set; }
    public string Identifier { get; set; }
    public string IdentifierHash { get; set; }
    public string OriginalIdentifier { get; set; }
    public string Salt { get; set; }
    public string VerifierId { get; set; }
    public string IsLoginGuardian { get; set; }
    public string TransactionId { get; set; }
}