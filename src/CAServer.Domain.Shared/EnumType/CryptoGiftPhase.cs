namespace CAServer.EnumType;

/**
 * the client doesn't register
 */
public enum CryptoGiftPhase
{
    Available = 0, //not claimed and not expired
    Expired = 1, //the red package is expired
    Claimed = 2, //the client has received the gift
    ExpiredReleased = 3, //the quota is expired, so it's released
    FullyClaimed = 4, //the red package has been fully claimed
    OnlyNewUsers = 5, //the crypto gift is only for new users
    GrabbedQuota = 6, //claimed but have enough quota
    NoQuota = 7, //there is no quota right now
    ClaimedVisited = 8, //claimed the crypto gift, visited more than one time
}