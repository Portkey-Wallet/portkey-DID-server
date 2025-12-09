namespace CAServer.CAAccount;

public interface IPoseidonIdentifierHashProvider
{
    public string GenerateIdentifierHash(string subject, byte[] salt);
}