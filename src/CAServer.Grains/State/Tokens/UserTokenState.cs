using CAServer.Tokens.Dtos;

namespace CAServer.Grains.State.Tokens;

public class UserTokenState
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool IsDisplay { get; set; }
    public bool IsDefault { get; set; }
    public int SortWeight { get; set; }
    public Token Token { get; set; }
    public bool IsDelete { get; set; }
}