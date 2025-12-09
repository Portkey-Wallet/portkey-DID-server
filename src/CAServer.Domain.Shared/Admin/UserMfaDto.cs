using Orleans;

namespace CAServer.Admin;

[GenerateSerializer]
public class UserMfaDto
{
    [Id(0)]
    public string GoogleTwoFactorAuthKey { get; set; }
    
    [Id(1)]
    public long LastModifyTime { get; set; }
    
}