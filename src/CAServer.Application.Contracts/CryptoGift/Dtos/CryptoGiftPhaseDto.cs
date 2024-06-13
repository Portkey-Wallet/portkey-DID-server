using CAServer.EnumType;

namespace CAServer.CryptoGift.Dtos;

public class CryptoGiftPhaseDto
{
    public CryptoGiftPhase CryptoGiftPhase { get; set; }
    
    public string Prompt { get; set; }
    
    public string SubPrompt { get; set; }
    
    public long Amount { get; set; }
    
    public string Memo { get; set; }
    
    public bool IsNewUsersOnly { get; set; }
    
    public long RemainingWaitingSeconds { get; set; }
    
    public long RemainingExpirationSeconds { get; set; }
    
    public UserInfoDto Sender { get; set; }
}

public class UserInfoDto
{
    public string Avatar { get; set; }
    
    public string Nickname { get; set; }
}