namespace CAServer.Tokens.Dtos;

public class AddUserTokenInput
{
    public bool IsDefault { get; set; }
    public bool IsDisplay { get; set; }
    public Token Token { get; set; }
}