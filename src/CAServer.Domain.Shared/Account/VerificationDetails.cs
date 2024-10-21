namespace CAServer.CAAccount.Dtos;

public class VerificationDetails
{
    public string Address { get; set; }
    public string PublicKey { get; set; }
    public string Signature { get; set; }
    public long Timestamp { get; set; }
    public string Extra { get; set; }
}