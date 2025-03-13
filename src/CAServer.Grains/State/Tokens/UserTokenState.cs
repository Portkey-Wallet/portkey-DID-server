using CAServer.Tokens.Dtos;

namespace CAServer.Grains.State.Tokens;

[GenerateSerializer]
public class UserTokenState
{
	[Id(0)]
    public Guid Id { get; set; }
	[Id(1)]
    public Guid UserId { get; set; }
	[Id(2)]
    public bool IsDisplay { get; set; }
	[Id(3)]
    public bool IsDefault { get; set; }
	[Id(4)]
    public int SortWeight { get; set; }
	[Id(5)]
    public Token Token { get; set; }
	[Id(6)]
    public bool IsDelete { get; set; }
}
