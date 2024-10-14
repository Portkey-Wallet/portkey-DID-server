using System;
using System.Collections.Generic;

namespace CAServer.Commons;

public static class AElfContractMethodName
{
    public const string GetHolderInfo = "GetHolderInfo";
    public const string GetVerifierServers = "GetVerifierServers";
    public const string GetBalance = "GetBalance";
    public const string GetTokenInfo = "GetTokenInfo";
    public const string ClaimToken = "ClaimToken";
    public const string Transfer = "Transfer";
    public const string AddManagerInfo = "AddManagerInfo";
    public const string AddGuardian = "AddGuardian";
    public const string CreateCAHolderOnNonCreateChain = "ReportPreCrossChainSyncHolderInfo";
    public const string SocialRecovery = "SocialRecovery";
    public const string ManagerForwardCall = "ManagerForwardCall";
    public const string Issue = "Issue";
    public const string CreateCAHolder = "CreateCAHolder";
    public const string RemoveGuardian = "RemoveGuardian";
    public const string UpdateGuardian = "UpdateGuardian";
    public const string RemoveOtherManagerInfo = "RemoveOtherManagerInfo";
    public const string SetGuardianForLogin = "SetGuardianForLogin";
    public const string UnsetGuardianForLogin = "UnsetGuardianForLogin";
    public const string SetTransferLimit = "SetTransferLimit";
    public const string RemoveManagerInfo = "RemoveManagerInfo";
    public const string GuardianApproveTransfer = "GuardianApproveTransfer";
    public const string VerifyZkLogin = "VerifyZkLogin";
    public const string VerifySignature = "VerifySignature";
    public static List<string> MethodNames = new List<string>()
    {
        CreateCAHolder,
        SocialRecovery,
        AddGuardian,
        RemoveGuardian,
        UpdateGuardian,
        RemoveOtherManagerInfo,
        SetGuardianForLogin,
        SetTransferLimit,
        UnsetGuardianForLogin,
        RemoveManagerInfo,
    };
}

public static class CommonConstant
{
    public const string EmptyString = "";
    public const string Dot = ".";
    public const string Hyphen = "-";
    public const string Colon = ":";
    public const string Underline = "_";
    public const string Comma = ",";
    public const string UpperZ = "z";
    public const string DefaultDappName = "Unknown";
    public const string CurrencyFiat = "Fiat";
    public const string CurrencyCrypto = "Crypto";

    public const string ResourceTokenKey = "ResourceToken";
    public const int CacheExpirationDays = 365;
    public const string CacheCorrectUserTokenBalancePre = "CorrectUserTokenBalance:{0}:{1}:{2}";
    public const string CacheTokenInfoPre = "CorrectTokenInfo:{0}:{1}";
    public const long CacheTokenBalanceExpirationSeconds = 60;

    public const string FiatListKey = "FiatList";
    public const string NftFiatListKey = "NftFiatList";
    public const int FiatListExpirationMinutes = 30;
    public const int CryptoListExpirationMinutes = 30;
    public const int OrderQuoteExpirationMinutes = 30;
    public static DateTimeOffset DefaultAbsoluteExpiration = DateTime.Parse("2099-01-01 12:00:00");
    public const string OrderStatusInfoPrefix = "OrderStatusInfo";
    public const string TreasuryOrderStatusInfoPrefix = "TreasuryOrderStatusInfo";

    public const string ChainName = "aelf";
    public const string ELF = "ELF";
    public const string USD = "USD";
    public const string USDT = "USDT";

    public const string MainChainId = "AELF";
    public const string TDVVChainId = "tDVV";
    public const string TDVWChainId = "tDVW";
    public const double DefaultAchFee = 0.39;
    public const double DefaultCrossChainFee = 0.35;
    public const double DefaultMaxFee = 0.39;
    public const double DefaultEtransferFee = 0.01;
    
    public const string AelfCoingeckoId = "aelf";
    public const string AelfSymbol = "ELF";
    public const string SgrCoingeckoId = "schrodinger-2";
    public const string SgrSymbol = "SGR";
    public const string SgrSymbolName = "SGR-1";

    public const string CryptoGiftProjectCode = "20000";

    public const string AppleTransferMessage =
        "We are currently upgrading our system to serve you better. During this period, the Apple ID service is temporarily unavailable.";

    public const string AuthHeader = "Authorization";
    public const string ImAuthHeader = "R-Authorization";
    public const string SuccessCode = "20000";

    public const string AppleRevokeUrl = "https://appleid.apple.com/auth/revoke";
    public const string ImFollowUrl = "api/v1/contacts/follow";
    public const string ImUnFollowUrl = "api/v1/contacts/unfollow";

    public const string UserExtraInfoIdPrefix = "UserExtraInfo-";

    public const string ApplicationName = "Portkey";
    public const string ModuleName = "Api";

    public const int WalletNameDefaultLength = 8;
    public const string AppVersionKeyPrefix = "AppVersion";
    public const string DefaultSymbol = "ELF";
    public const string TransferCard = "transfer-card";

    public const string DefaultFiatUSD = "USD";
    public const string DefaultCryptoELF = "ELF";
    public const string DefaultFiatPrice = "200";
    public const string DefaultCryptoPrice = "400";

    public const string ProtocolName = "http";

    public const string UserGrowthPrefix = "UserGrowth";
    public const string RedDotPrefix = "RedDot";
    public const int InitInviteCode = 10000;
    public const string InviteCodeGrainId = "UserGrowth-InviteCode";
    public const string UrlSegmentation = "?";
    public const string UpgradeGrainIdPrefix = "UpgradeInfo";
    public const string GetUserExtraInfoUri = "api/app/userExtraInfo";

    public const string CrossChainTransferMethodName = "CrossChainTransfer";
    
    public const string TwitterTokenUrl = "https://api.twitter.com/2/oauth2/token";
    public const string TwitterUserInfoUrl = "https://api.twitter.com/2/users/me";
    public const string JwtTokenPrefix = "Bearer";
    public const int TwitterLimitCount = 200;
    
    public const string ActivitiesStartVersion = "1.17.0";
    public const string NftToFtStartVersion = "1.18.0";

    public const string ReferralKey = "Portkey:ReferralBank";
    public const string HamsterRankKey = "Portkey:HamsterBank";
    

    public const string DefaultReferralActivityStartTime = "2024-06-27 00:00:00";

    public const string SingUp = " created a Portkey account";
    public const string HamsterScore = " collected {0} $ACORNS";
    
    
    public const int InitTokenId = 10;
    public const string FreeMintTokenIdGrainId = "FreeMint-TokenId";
    public const int FreeMintTotalSupply = 1;
    public const int FreeMintDecimals = 0;

    public const string PortkeyS3Mark = "did";
    public const string ImS3Mark = "im";

    public const string HamsterPassSymbol = "HAMSTERPASS-1";
    public const string HamsterKingSymbol = "KINGHAMSTER-1";

    public const string VersionName = "Version";
    
    public const string TokenInfoCachePrefix = "TokenInfo";
    public const string SyncStateUri = "apps/sync-state";
    public const string ReplaceUri = "app/graphql";
}