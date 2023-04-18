using CAServer.Account;

namespace CAServer.Grains.State;

public class RegisterState
{
    public string Id { get; set; }
    public List<RegisterInfo> RegisterInfo { get; set; } = new();
}

public class RegisterInfo : CAAccountBase
{
    public string GrainId { get; set; }
    public GuardianAccountInfo GuardianAccountInfo { get; set; }
    public DateTime? RegisteredTime { get; set; }
    public bool? RegisterSuccess { get; set; }
    public string RegisterMessage { get; set; }
}