using CAServer.EnumType;
using CAServer.UserAssets.Dtos;

namespace CAServer.CryptoGift.Dtos;

public class CryptoGiftPhaseDto
{
    public CryptoGiftPhase CryptoGiftPhase { get; set; }
    
    public string Prompt { get; set; }
    
    public string SubPrompt { get; set; }
    
    public long Amount { get; set; }
    
    public int Decimals { get; set; }
    
    public string Symbol { get; set; }
    
    public string Label { get; set; }
    
    public string DollarValue { get; set; }
    
    public string NftAlias { get; set; }
    
    public string NftTokenId { get; set; }
    
    public string NftImageUrl { get; set; }
    
    public int AssetType { get; set; }
    
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