namespace CAServer.Grains.State.UserExtraInfo;

[GenerateSerializer]
public class UserExtraInfoState
{
	[Id(0)]
    public string Id { get; set; }
	[Id(1)]
    public string FullName { get; set; }
	[Id(2)]
    public string FirstName { get; set; }
	[Id(3)]
    public string LastName { get; set; }
	[Id(4)]
    public string Email { get; set; }
	[Id(5)]
    public string Picture { get; set; }
	[Id(6)]
    public bool VerifiedEmail { get; set; }
	[Id(7)]
    public bool IsPrivateEmail { get; set; }
	[Id(8)]
    public string GuardianType { get; set; }
	[Id(9)]
    public DateTime AuthTime { get; set; }
}
