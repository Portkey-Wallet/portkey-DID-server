namespace CAServer.EnumType;

/**
 * the client doesn't register
 */
public enum CryptoGiftPhase
{
    Available, //not claimed and not expired
    FullyClaimed,
    Expired,
    NoQuota,
    GrabbedQuota, //claimed but have enough quota
    Claimed, //the client has received the gift
    ExpiredReleased, //the quota is expired, so it's released
}