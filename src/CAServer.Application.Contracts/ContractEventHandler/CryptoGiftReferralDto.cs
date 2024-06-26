using CAServer.CAAccount.Dtos;

namespace CAServer.ContractEventHandler;

public class CryptoGiftReferralDto
{
    public string CaHash { get; set; }
    public string CaAddress { get; set; }
    public ReferralInfo ReferralInfo { get; set; }
    public bool IsNewUser { get; set; }
    public string IpAddress { get; set; }
}