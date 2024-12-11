using CAServer.Account;
using CAServer.Dtos;
using CAServer.CAAccount.Dtos;

namespace CAServer.Grains.State;

[GenerateSerializer]
public class RegisterState
{
	[Id(0)]
    public string Id { get; set; }
	[Id(1)]
    public List<RegisterInfo> RegisterInfo { get; set; } = new();
}

[GenerateSerializer]
public class RegisterInfo : CAAccountBase
{
	[Id(0)]
    public string GrainId { get; set; }
	[Id(1)]
    public GuardianInfo GuardianInfo { get; set; }
	[Id(2)]
    public DateTime? RegisteredTime { get; set; }
	[Id(3)]
    public bool? RegisterSuccess { get; set; }
	[Id(4)]
    public string RegisterMessage { get; set; }
	[Id(5)]
    public ProjectDelegateInfo ProjectDelegateInfo { get; set; }
	[Id(6)]
    public ReferralInfo ReferralInfo { get; set; }
}
