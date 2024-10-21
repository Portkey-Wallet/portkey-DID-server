namespace CAServer.Verifier;

public class OperationDetailsDto
{
    public string Manager { get; set; }
    public string RemoveManager { get; set; }
    public string IdentifierHash { get; set; }
    public int GuardianType { get; set; } = -1;
    public string VerifierId { get; set; }
    public string PreVerifierId { get; set; }
    public string NewVerifierId { get; set; }
    public string To { get; set; }
    public string Symbol { get; set; }
    public string Amount { get; set; }
    public string Spender { get; set; }
    public long SingleLimit { get; set; }
    public long DailyLimit { get; set; }
}