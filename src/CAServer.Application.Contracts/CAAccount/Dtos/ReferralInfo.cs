using Orleans;

namespace CAServer.CAAccount.Dtos;

[GenerateSerializer]
public class ReferralInfo
{
    [Id(0)]
    public string ReferralCode { get; set; }
    
    [Id(1)]
    public string ProjectCode { get; set; }
    
    [Id(2)]
    public string Random { get; set; }
}