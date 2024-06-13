namespace CAServer.EnumType;

/**
 * the client doesn't register
 */
public enum CryptoGiftPhase
{
    Available = 0, //not claimed and not expired
    Expired, //the red package is expired
    Claimed, //the client has received the gift
    ExpiredReleased, //the quota is expired, so it's released
    FullyClaimed, //the red package has been fully claimed
    OnlyNewUsers, //the crypto gift is only for new users
    GrabbedQuota, //claimed but have enough quota
    NoQuota, //
}